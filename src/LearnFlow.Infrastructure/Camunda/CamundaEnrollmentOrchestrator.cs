using LearnFlow.Application.Abstractions;
using LearnFlow.Domain.Entities;

namespace LearnFlow.Infrastructure.Camunda;

public sealed class CamundaEnrollmentOrchestrator(CamundaRestClient client)
    : IEnrollmentWorkflowOrchestrator
{
    public Task<string?> StartAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        => client.StartProcessAsync(enrollmentId, cancellationToken);

    public Task CompleteApprovalAsync(
        EnrollmentRequest enrollment,
        bool approved,
        string reviewer,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(enrollment.ProcessInstanceKey))
            throw new InvalidOperationException("The enrollment does not have a Camunda process instance.");
        return client.CompleteApprovalAsync(
            enrollment.ProcessInstanceKey,
            approved,
            reviewer,
            cancellationToken);
    }
}
