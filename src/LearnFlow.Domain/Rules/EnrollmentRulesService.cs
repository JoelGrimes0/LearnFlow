using LearnFlow.Domain.Entities;

namespace LearnFlow.Domain.Rules;

public sealed class EnrollmentRulesService : IEnrollmentRulesService
{
    private static readonly EnrollmentStatus[] ActiveStatuses =
    [
        EnrollmentStatus.Submitted,
        EnrollmentStatus.Validating,
        EnrollmentStatus.AwaitingApproval,
        EnrollmentStatus.Approved,
        EnrollmentStatus.Enrolled
    ];

    public EnrollmentDecision Evaluate(
        Learner learner,
        Course course,
        IReadOnlyCollection<EnrollmentRequest> existingEnrollments)
    {
        var duplicate = existingEnrollments.Any(enrollment =>
            enrollment.LearnerId == learner.Id &&
            enrollment.CourseId == course.Id &&
            ActiveStatuses.Contains(enrollment.Status));

        var enrolledCount = existingEnrollments.Count(enrollment =>
            enrollment.CourseId == course.Id &&
            enrollment.Status == EnrollmentStatus.Enrolled);

        var missingPrerequisites = course.PrerequisiteCodes
            .Except(learner.CompletedCourseCodes, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var checks = new List<RuleCheck>
        {
            new("Course is active", course.IsActive,
                course.IsActive ? "Course is open for enrollment." : "Course is not currently offered."),
            new("Enrollment is not duplicated", !duplicate,
                duplicate ? "Learner already has an active enrollment request." : "No active duplicate was found."),
            new("Seat is available", enrolledCount < course.Capacity,
                enrolledCount < course.Capacity
                    ? $"{course.Capacity - enrolledCount} seat(s) remain."
                    : "The course has reached capacity."),
            new("Prerequisites are satisfied", missingPrerequisites.Length == 0,
                missingPrerequisites.Length == 0
                    ? "All required courses are complete."
                    : $"Missing: {string.Join(", ", missingPrerequisites)}")
        };

        return new EnrollmentDecision(
            checks.All(check => check.Passed),
            course.RequiresManagerApproval,
            checks);
    }
}
