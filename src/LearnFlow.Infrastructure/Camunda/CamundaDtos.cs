using System.Text.Json;

namespace LearnFlow.Infrastructure.Camunda;

public sealed record CamundaJob(
    string JobKey,
    string Type,
    string ProcessInstanceKey,
    int Retries,
    JsonElement Variables);

public sealed record CamundaJobActivationResponse(IReadOnlyList<CamundaJob> Jobs);

public sealed record CamundaSearchResponse<T>(IReadOnlyList<T> Items);

public sealed record CamundaUserTask(
    string UserTaskKey,
    string ProcessInstanceKey,
    string State,
    string Name);
