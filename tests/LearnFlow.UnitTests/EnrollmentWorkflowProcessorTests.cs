using LearnFlow.Application.Abstractions;
using LearnFlow.Application.Services;
using LearnFlow.Domain.Entities;
using LearnFlow.Domain.Rules;
using Moq;

namespace LearnFlow.UnitTests;

public sealed class EnrollmentWorkflowProcessorTests
{
    [Fact]
    public async Task ValidateAsync_WhenApprovalIsRequired_MovesRequestToAwaitingApproval()
    {
        var learner = new Learner { Id = Guid.NewGuid(), Name = "Maya" };
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Code = "BPM-810",
            Title = "Camunda",
            Capacity = 20,
            IsActive = true,
            RequiresManagerApproval = true
        };
        var enrollment = new EnrollmentRequest
        {
            LearnerId = learner.Id,
            CourseId = course.Id
        };
        var repository = Repository(learner, course, enrollment);
        var processor = new EnrollmentWorkflowProcessor(repository.Object, new EnrollmentRulesService());

        var variables = await processor.ValidateAsync(enrollment.Id);

        Assert.Equal(EnrollmentStatus.AwaitingApproval, enrollment.Status);
        Assert.True((bool)variables["eligible"]);
        Assert.True((bool)variables["requiresApproval"]);
        repository.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteEnrollmentAsync_RecordsEnrollmentAndAuditEvent()
    {
        var learner = new Learner { Id = Guid.NewGuid(), Name = "Maya" };
        var course = new Course { Id = Guid.NewGuid(), Code = "TST-420", Capacity = 20 };
        var enrollment = new EnrollmentRequest
        {
            LearnerId = learner.Id,
            CourseId = course.Id,
            Status = EnrollmentStatus.Approved
        };
        var repository = Repository(learner, course, enrollment);
        var processor = new EnrollmentWorkflowProcessor(repository.Object, new EnrollmentRulesService());

        await processor.CompleteEnrollmentAsync(enrollment.Id);

        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
        Assert.Contains(enrollment.History, item => item.Type == "enrollment.completed");
    }

    private static Mock<ILearningRepository> Repository(
        Learner learner,
        Course course,
        EnrollmentRequest enrollment)
    {
        var repository = new Mock<ILearningRepository>();
        repository.Setup(item => item.GetLearnerAsync(learner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(learner);
        repository.Setup(item => item.GetCourseAsync(course.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(course);
        repository.Setup(item => item.GetEnrollmentAsync(enrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollment);
        repository.Setup(item => item.GetEnrollmentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([enrollment]);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repository;
    }
}
