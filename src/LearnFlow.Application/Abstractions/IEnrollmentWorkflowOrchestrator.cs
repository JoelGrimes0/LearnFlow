using LearnFlow.Domain.Entities;

namespace LearnFlow.Application.Abstractions;

public interface IEnrollmentWorkflowOrchestrator
{
    Task<string?> StartAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task CompleteApprovalAsync(
        EnrollmentRequest enrollment,
        bool approved,
        string reviewer,
        CancellationToken cancellationToken = default);
}
