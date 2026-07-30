using LearnFlow.Application.Abstractions;
using LearnFlow.Application.Services;
using LearnFlow.Domain.Entities;

namespace LearnFlow.Infrastructure.Camunda;

public sealed class DemoEnrollmentOrchestrator(EnrollmentWorkflowProcessor processor)
    : IEnrollmentWorkflowOrchestrator
{
    public async Task<string?> StartAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        var result = await processor.ValidateAsync(enrollmentId, cancellationToken);
        var eligible = (bool)result["eligible"];
        var requiresApproval = (bool)result["requiresApproval"];
        if (eligible && !requiresApproval)
        {
            await processor.CompleteEnrollmentAsync(enrollmentId, cancellationToken);
            await processor.NotifyAsync(enrollmentId, cancellationToken);
        }
        else if (!eligible)
        {
            await processor.RejectAsync(enrollmentId, cancellationToken);
            await processor.NotifyAsync(enrollmentId, cancellationToken);
        }
        return $"demo-{enrollmentId:N}";
    }

    public async Task CompleteApprovalAsync(
        EnrollmentRequest enrollment,
        bool approved,
        string reviewer,
        CancellationToken cancellationToken = default)
    {
        if (approved)
            await processor.CompleteEnrollmentAsync(enrollment.Id, cancellationToken);
        else
            await processor.RejectAsync(enrollment.Id, cancellationToken);
        await processor.NotifyAsync(enrollment.Id, cancellationToken);
    }
}
