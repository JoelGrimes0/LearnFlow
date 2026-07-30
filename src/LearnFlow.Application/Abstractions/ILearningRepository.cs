using LearnFlow.Domain.Entities;

namespace LearnFlow.Application.Abstractions;

public interface ILearningRepository
{
    Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default);
    Task<Course?> GetCourseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Learner>> GetLearnersAsync(CancellationToken cancellationToken = default);
    Task<Learner?> GetLearnerAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentRequest>> GetEnrollmentsAsync(CancellationToken cancellationToken = default);
    Task<EnrollmentRequest?> GetEnrollmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddEnrollmentAsync(EnrollmentRequest enrollment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
