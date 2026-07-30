namespace LearnFlow.Domain.Entities;

public sealed class Learner
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public IReadOnlyList<string> CompletedCourseCodes { get; init; } = [];
}
