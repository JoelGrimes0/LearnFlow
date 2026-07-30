using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LearnFlow.Infrastructure.Camunda;

public sealed class CamundaDeploymentService(
    CamundaRestClient client,
    CamundaOptions options,
    IHostEnvironment environment,
    ILogger<CamundaDeploymentService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(options.BpmnPath, environment.ContentRootPath);
        if (!await client.IsHealthyAsync(cancellationToken))
        {
            logger.LogWarning("Camunda is unavailable. BPMN deployment was skipped.");
            return;
        }

        await client.DeployAsync(path, cancellationToken);
        logger.LogInformation("Deployed LearnFlow enrollment process from {BpmnPath}", path);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
