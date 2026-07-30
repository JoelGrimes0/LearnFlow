using System.Text.Json;
using LearnFlow.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LearnFlow.Infrastructure.Camunda;

public sealed class CamundaJobWorker(
    CamundaRestClient client,
    EnrollmentWorkflowProcessor processor,
    ILogger<CamundaJobWorker> logger) : BackgroundService
{
    private static readonly string[] JobTypes =
    [
        "validate-enrollment",
        "complete-enrollment",
        "reject-enrollment",
        "notify-learner"
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var type in JobTypes)
                    await ProcessJobsAsync(type, stoppingToken);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(exception, "Camunda worker could not reach the orchestration cluster.");
            }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessJobsAsync(string type, CancellationToken cancellationToken)
    {
        var jobs = await client.ActivateJobsAsync(type, cancellationToken);
        foreach (var job in jobs)
        {
            try
            {
                var enrollmentId = ReadEnrollmentId(job.Variables);
                IReadOnlyDictionary<string, object> variables = type switch
                {
                    "validate-enrollment" => await processor.ValidateAsync(enrollmentId, cancellationToken),
                    "complete-enrollment" => await processor.CompleteEnrollmentAsync(enrollmentId, cancellationToken),
                    "reject-enrollment" => await processor.RejectAsync(enrollmentId, cancellationToken),
                    "notify-learner" => await processor.NotifyAsync(enrollmentId, cancellationToken),
                    _ => throw new InvalidOperationException($"Unsupported job type {type}.")
                };
                await client.CompleteJobAsync(job.JobKey, variables, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Camunda job {JobKey} failed.", job.JobKey);
                await client.FailJobAsync(
                    job.JobKey,
                    job.Retries - 1,
                    exception.Message,
                    cancellationToken);
            }
        }
    }

    private static Guid ReadEnrollmentId(JsonElement variables)
    {
        if (variables.ValueKind == JsonValueKind.Object &&
            variables.TryGetProperty("enrollmentId", out var value) &&
            Guid.TryParse(value.GetString(), out var id))
            return id;
        throw new InvalidOperationException("The job does not contain a valid enrollmentId variable.");
    }
}
