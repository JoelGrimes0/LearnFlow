using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LearnFlow.Infrastructure.Camunda;

public sealed class CamundaRestClient(HttpClient httpClient, CamundaOptions options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync("topology", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task DeployAsync(string bpmnPath, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(bpmnPath);
        using var content = new MultipartFormDataContent();
        using var file = new StreamContent(stream);
        file.Headers.ContentType = new("application/xml");
        content.Add(file, "resources", Path.GetFileName(bpmnPath));
        using var response = await httpClient.PostAsync("deployments", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<string?> StartProcessAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            processDefinitionId = options.ProcessDefinitionId,
            processDefinitionVersion = -1,
            variables = new { enrollmentId = enrollmentId.ToString() }
        };
        using var response = await httpClient.PostAsJsonAsync(
            "process-instances",
            payload,
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<JsonObject>(JsonOptions, cancellationToken);
        return result?["processInstanceKey"]?.ToString();
    }

    public async Task<IReadOnlyList<CamundaJob>> ActivateJobsAsync(
        string jobType,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            type = jobType,
            worker = options.WorkerName,
            timeout = 30_000,
            maxJobsToActivate = 10,
            fetchVariable = new[] { "enrollmentId" }
        };
        using var response = await httpClient.PostAsJsonAsync(
            "jobs/activation",
            payload,
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<CamundaJobActivationResponse>(
            JsonOptions,
            cancellationToken);
        return result?.Jobs ?? [];
    }

    public async Task CompleteJobAsync(
        string jobKey,
        IReadOnlyDictionary<string, object> variables,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"jobs/{jobKey}/completion",
            new { variables },
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task FailJobAsync(
        string jobKey,
        int retries,
        string message,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"jobs/{jobKey}/failure",
            new
            {
                retries = Math.Max(0, retries),
                errorMessage = message,
                retryBackOff = 5_000
            },
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CompleteApprovalAsync(
        string processInstanceKey,
        bool approved,
        string reviewer,
        CancellationToken cancellationToken = default)
    {
        CamundaUserTask? task = null;
        for (var attempt = 0; attempt < 5 && task is null; attempt++)
        {
            using var searchResponse = await httpClient.PostAsJsonAsync(
                "user-tasks/search",
                new
                {
                    filter = new
                    {
                        processInstanceKey,
                        state = "CREATED"
                    },
                    page = new { limit = 20 }
                },
                JsonOptions,
                cancellationToken);
            await EnsureSuccessAsync(searchResponse, cancellationToken);
            var result = await searchResponse.Content.ReadFromJsonAsync<CamundaSearchResponse<CamundaUserTask>>(
                JsonOptions,
                cancellationToken);
            task = result?.Items.FirstOrDefault();
            if (task is null) await Task.Delay(500, cancellationToken);
        }

        if (task is null)
            throw new InvalidOperationException("Camunda approval task has not become available yet.");

        using var response = await httpClient.PostAsJsonAsync(
            $"user-tasks/{task.UserTaskKey}/completion",
            new { variables = new { approved, reviewedBy = reviewer } },
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Camunda returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }
}
