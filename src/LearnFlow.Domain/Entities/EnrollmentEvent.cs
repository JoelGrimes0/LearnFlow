namespace LearnFlow.Domain.Entities;

public sealed record EnrollmentEvent(
    DateTimeOffset OccurredAt,
    string Type,
    string Message);
