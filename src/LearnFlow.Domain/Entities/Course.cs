namespace LearnFlow.Domain.Entities;

public sealed class Course
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Instructor { get; init; } = string.Empty;
    public int DurationHours { get; init; }
    public int Capacity { get; init; }
    public bool IsActive { get; init; } = true;
    public bool RequiresManagerApproval { get; init; }
    public string Accent { get; init; } = "blue";
    public IReadOnlyList<string> PrerequisiteCodes { get; init; } = [];
}
