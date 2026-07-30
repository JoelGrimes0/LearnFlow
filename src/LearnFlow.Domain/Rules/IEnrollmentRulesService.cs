using LearnFlow.Domain.Entities;

namespace LearnFlow.Domain.Rules;

public interface IEnrollmentRulesService
{
    EnrollmentDecision Evaluate(
        Learner learner,
        Course course,
        IReadOnlyCollection<EnrollmentRequest> existingEnrollments);
}
