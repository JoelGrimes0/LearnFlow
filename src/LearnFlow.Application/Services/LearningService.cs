using LearnFlow.Application.Abstractions;
using LearnFlow.Application.Contracts;
using LearnFlow.Domain.Entities;

namespace LearnFlow.Application.Services;

public sealed class LearningService(
    ILearningRepository repository,
    IEnrollmentWorkflowOrchestrator workflow)
{
    public async Task<IReadOnlyList<CourseDto>> GetCoursesAsync(CancellationToken cancellationToken = default)
    {
        var courses = await repository.GetCoursesAsync(cancellationToken);
        var enrollments = await repository.GetEnrollmentsAsync(cancellationToken);
        return courses.Select(course => MapCourse(course, enrollments)).ToArray();
    }

    public async Task<IReadOnlyList<LearnerDto>> GetLearnersAsync(CancellationToken cancellationToken = default)
        => (await repository.GetLearnersAsync(cancellationToken))
            .Select(learner => new LearnerDto(
                learner.Id,
                learner.Name,
                learner.Email,
                learner.Department,
                learner.CompletedCourseCodes))
            .ToArray();

    public async Task<IReadOnlyList<EnrollmentDto>> GetEnrollmentsAsync(CancellationToken cancellationToken = default)
    {
        var courses = await repository.GetCoursesAsync(cancellationToken);
        var learners = await repository.GetLearnersAsync(cancellationToken);
        return (await repository.GetEnrollmentsAsync(cancellationToken))
            .OrderByDescending(enrollment => enrollment.SubmittedAt)
            .Select(enrollment => MapEnrollment(enrollment, learners, courses))
            .ToArray();
    }

    public async Task<EnrollmentDto?> GetEnrollmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetEnrollmentAsync(id, cancellationToken);
        if (enrollment is null) return null;
        return MapEnrollment(
            enrollment,
            await repository.GetLearnersAsync(cancellationToken),
            await repository.GetCoursesAsync(cancellationToken));
    }

    public async Task<EnrollmentDto> SubmitAsync(
        SubmitEnrollmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var learner = await repository.GetLearnerAsync(request.LearnerId, cancellationToken)
            ?? throw new ArgumentException("The selected learner does not exist.");
        var course = await repository.GetCourseAsync(request.CourseId, cancellationToken)
            ?? throw new ArgumentException("The selected course does not exist.");

        var enrollment = new EnrollmentRequest
        {
            LearnerId = learner.Id,
            CourseId = course.Id
        };
        enrollment.History.Add(new(DateTimeOffset.UtcNow, "request.submitted", "Enrollment request submitted."));
        await repository.AddEnrollmentAsync(enrollment, cancellationToken);
        enrollment.ProcessInstanceKey = await workflow.StartAsync(enrollment.Id, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return MapEnrollment(enrollment, [learner], [course]);
    }

    public async Task<EnrollmentDto> ApproveAsync(
        Guid id,
        ApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetEnrollmentAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Enrollment request was not found.");
        if (enrollment.Status != EnrollmentStatus.AwaitingApproval)
            throw new InvalidOperationException("Only enrollment requests awaiting approval can be reviewed.");

        enrollment.History.Add(new(
            DateTimeOffset.UtcNow,
            request.Approved ? "approval.granted" : "approval.denied",
            $"{request.Reviewer} {(request.Approved ? "approved" : "denied")} the enrollment."));
        await repository.SaveChangesAsync(cancellationToken);
        await workflow.CompleteApprovalAsync(enrollment, request.Approved, request.Reviewer, cancellationToken);

        return MapEnrollment(
            enrollment,
            await repository.GetLearnersAsync(cancellationToken),
            await repository.GetCoursesAsync(cancellationToken));
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var courses = await repository.GetCoursesAsync(cancellationToken);
        var learners = await repository.GetLearnersAsync(cancellationToken);
        var enrollments = await repository.GetEnrollmentsAsync(cancellationToken);
        var completed = enrollments.Count(item => item.Status == EnrollmentStatus.Enrolled);
        var decided = enrollments.Count(item =>
            item.Status is EnrollmentStatus.Enrolled or EnrollmentStatus.Rejected);
        var recent = enrollments
            .OrderByDescending(item => item.SubmittedAt)
            .Take(5)
            .Select(item => MapEnrollment(item, learners, courses))
            .ToArray();

        return new DashboardDto(
            courses.Count(course => course.IsActive),
            learners.Count,
            completed,
            enrollments.Count(item => item.Status == EnrollmentStatus.AwaitingApproval),
            decided == 0 ? 0 : Math.Round((decimal)completed / decided * 100, 1),
            recent);
    }

    private static CourseDto MapCourse(Course course, IReadOnlyList<EnrollmentRequest> enrollments)
        => new(
            course.Id,
            course.Code,
            course.Title,
            course.Description,
            course.Category,
            course.Instructor,
            course.DurationHours,
            course.Capacity,
            enrollments.Count(item =>
                item.CourseId == course.Id && item.Status == EnrollmentStatus.Enrolled),
            course.IsActive,
            course.RequiresManagerApproval,
            course.Accent,
            course.PrerequisiteCodes);

    private static EnrollmentDto MapEnrollment(
        EnrollmentRequest enrollment,
        IReadOnlyList<Learner> learners,
        IReadOnlyList<Course> courses)
    {
        var learner = learners.Single(item => item.Id == enrollment.LearnerId);
        var course = courses.Single(item => item.Id == enrollment.CourseId);
        return new EnrollmentDto(
            enrollment.Id,
            learner.Id,
            learner.Name,
            learner.Department,
            course.Id,
            course.Code,
            course.Title,
            enrollment.Status.ToString(),
            enrollment.SubmittedAt,
            enrollment.ProcessInstanceKey,
            enrollment.RuleChecks,
            enrollment.History.OrderByDescending(item => item.OccurredAt).ToArray());
    }
}
