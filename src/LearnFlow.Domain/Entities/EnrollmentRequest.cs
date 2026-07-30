namespace LearnFlow.Domain.Entities;

public sealed class EnrollmentRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid LearnerId { get; init; }
    public Guid CourseId { get; init; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Submitted;
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? ProcessInstanceKey { get; set; }
    public List<RuleCheck> RuleChecks { get; set; } = [];
    public List<EnrollmentEvent> History { get; } = [];
}

public sealed record RuleCheck(string Rule, bool Passed, string Detail);
