using LearnFlow.Domain.Entities;

namespace LearnFlow.Application.Contracts;

public sealed record SubmitEnrollmentRequest(Guid LearnerId, Guid CourseId);
public sealed record ApprovalRequest(bool Approved, string Reviewer);

public sealed record CourseDto(
    Guid Id,
    string Code,
    string Title,
    string Description,
    string Category,
    string Instructor,
    int DurationHours,
    int Capacity,
    int Enrolled,
    bool IsActive,
    bool RequiresManagerApproval,
    string Accent,
    IReadOnlyList<string> PrerequisiteCodes);

public sealed record LearnerDto(
    Guid Id,
    string Name,
    string Email,
    string Department,
    IReadOnlyList<string> CompletedCourseCodes);

public sealed record EnrollmentDto(
    Guid Id,
    Guid LearnerId,
    string Learner,
    string Department,
    Guid CourseId,
    string CourseCode,
    string Course,
    string Status,
    DateTimeOffset SubmittedAt,
    string? ProcessInstanceKey,
    IReadOnlyList<RuleCheck> RuleChecks,
    IReadOnlyList<EnrollmentEvent> History);

public sealed record DashboardDto(
    int ActiveCourses,
    int TotalLearners,
    int EnrolledLearners,
    int PendingApprovals,
    decimal CompletionRate,
    IReadOnlyList<EnrollmentDto> RecentEnrollments);
