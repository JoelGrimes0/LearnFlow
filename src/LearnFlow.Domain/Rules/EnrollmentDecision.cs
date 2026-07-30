using LearnFlow.Domain.Entities;

namespace LearnFlow.Domain.Rules;

public sealed record EnrollmentDecision(
    bool Eligible,
    bool RequiresApproval,
    IReadOnlyList<RuleCheck> Checks);
