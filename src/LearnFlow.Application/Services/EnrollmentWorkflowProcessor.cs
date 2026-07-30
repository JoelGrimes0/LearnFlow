using LearnFlow.Application.Abstractions;
using LearnFlow.Domain.Entities;
using LearnFlow.Domain.Rules;

namespace LearnFlow.Application.Services;

public sealed class EnrollmentWorkflowProcessor(
    ILearningRepository repository,
    IEnrollmentRulesService rules)
{
    public async Task<IReadOnlyDictionary<string, object>> ValidateAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await RequireEnrollmentAsync(enrollmentId, cancellationToken);
        var learner = await repository.GetLearnerAsync(enrollment.LearnerId, cancellationToken)
            ?? throw new InvalidOperationException("Learner was not found.");
        var course = await repository.GetCourseAsync(enrollment.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");
        var enrollments = await repository.GetEnrollmentsAsync(cancellationToken);

        enrollment.Status = EnrollmentStatus.Validating;
        enrollment.History.Add(new(DateTimeOffset.UtcNow, "rules.started", "Enrollment rules evaluation started."));

        var decision = rules.Evaluate(
            learner,
            course,
            enrollments.Where(item => item.Id != enrollment.Id).ToArray());

        enrollment.RuleChecks = decision.Checks.ToList();
        enrollment.Status = !decision.Eligible
            ? EnrollmentStatus.Rejected
            : decision.RequiresApproval
                ? EnrollmentStatus.AwaitingApproval
                : EnrollmentStatus.Approved;

        enrollment.History.Add(new(
            DateTimeOffset.UtcNow,
            decision.Eligible ? "rules.passed" : "rules.failed",
            decision.Eligible
                ? "Business requirements were satisfied."
                : "Enrollment was rejected because one or more business requirements failed."));

        await repository.SaveChangesAsync(cancellationToken);

        return new Dictionary<string, object>
        {
            ["eligible"] = decision.Eligible,
            ["requiresApproval"] = decision.RequiresApproval,
            ["enrollmentId"] = enrollment.Id.ToString()
        };
    }

    public async Task<IReadOnlyDictionary<string, object>> CompleteEnrollmentAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await RequireEnrollmentAsync(enrollmentId, cancellationToken);
        enrollment.Status = EnrollmentStatus.Enrolled;
        enrollment.History.Add(new(DateTimeOffset.UtcNow, "enrollment.completed", "Learner was enrolled in the course."));
        await repository.SaveChangesAsync(cancellationToken);
        return new Dictionary<string, object> { ["enrollmentCompleted"] = true };
    }

    public async Task<IReadOnlyDictionary<string, object>> RejectAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await RequireEnrollmentAsync(enrollmentId, cancellationToken);
        enrollment.Status = EnrollmentStatus.Rejected;
        enrollment.History.Add(new(DateTimeOffset.UtcNow, "enrollment.rejected", "Enrollment request was closed."));
        await repository.SaveChangesAsync(cancellationToken);
        return new Dictionary<string, object> { ["enrollmentCompleted"] = false };
    }

    public async Task<IReadOnlyDictionary<string, object>> NotifyAsync(
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await RequireEnrollmentAsync(enrollmentId, cancellationToken);
        enrollment.History.Add(new(
            DateTimeOffset.UtcNow,
            "notification.sent",
            $"Enrollment outcome notification recorded for status {enrollment.Status}."));
        await repository.SaveChangesAsync(cancellationToken);
        return new Dictionary<string, object> { ["notificationSent"] = true };
    }

    private async Task<EnrollmentRequest> RequireEnrollmentAsync(
        Guid id,
        CancellationToken cancellationToken)
        => await repository.GetEnrollmentAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Enrollment {id} was not found.");
}
