namespace LearnFlow.Infrastructure.Camunda;

public sealed class CamundaOptions
{
    public const string SectionName = "Camunda";
    public bool Enabled { get; init; } = false;
    public string BaseUrl { get; init; } = "http://localhost:8080/v2/";
    public string ProcessDefinitionId { get; init; } = "learnflow-course-enrollment";
    public string BpmnPath { get; init; } = "../../process/course-enrollment.bpmn";
    public string WorkerName { get; init; } = "learnflow-dotnet-worker";
}
