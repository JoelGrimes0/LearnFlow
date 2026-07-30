using LearnFlow.Domain.Entities;
using LearnFlow.Domain.Rules;

namespace LearnFlow.UnitTests;

public sealed class EnrollmentRulesServiceTests
{
    private readonly EnrollmentRulesService _rules = new();
    private readonly Learner _learner = new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Learner",
        CompletedCourseCodes = ["PREREQ-101"]
    };

    [Fact]
    public void Evaluate_WhenEveryBusinessRequirementPasses_ReturnsEligible()
    {
        var result = _rules.Evaluate(_learner, Course(), []);

        Assert.True(result.Eligible);
        Assert.All(result.Checks, check => Assert.True(check.Passed));
    }

    [Fact]
    public void Evaluate_WhenCourseIsInactive_RejectsEnrollment()
    {
        var result = _rules.Evaluate(_learner, Course(isActive: false), []);

        Assert.False(result.Eligible);
        Assert.False(result.Checks.Single(check => check.Rule == "Course is active").Passed);
    }

    [Fact]
    public void Evaluate_WhenActiveDuplicateExists_RejectsEnrollment()
    {
        var course = Course();
        var existing = new EnrollmentRequest
        {
            LearnerId = _learner.Id,
            CourseId = course.Id,
            Status = EnrollmentStatus.AwaitingApproval
        };

        var result = _rules.Evaluate(_learner, course, [existing]);

        Assert.False(result.Eligible);
        Assert.Contains(result.Checks, check =>
            check.Rule == "Enrollment is not duplicated" && !check.Passed);
    }

    [Fact]
    public void Evaluate_WhenCourseIsFull_RejectsEnrollment()
    {
        var course = Course(capacity: 1);
        var existing = new EnrollmentRequest
        {
            LearnerId = Guid.NewGuid(),
            CourseId = course.Id,
            Status = EnrollmentStatus.Enrolled
        };

        var result = _rules.Evaluate(_learner, course, [existing]);

        Assert.False(result.Eligible);
        Assert.Contains(result.Checks, check => check.Rule == "Seat is available" && !check.Passed);
    }

    [Fact]
    public void Evaluate_WhenPrerequisiteIsMissing_ExplainsMissingCourse()
    {
        var course = Course(prerequisites: ["PREREQ-101", "SEC-220"]);

        var result = _rules.Evaluate(_learner, course, []);

        var prerequisite = result.Checks.Single(check => check.Rule == "Prerequisites are satisfied");
        Assert.False(result.Eligible);
        Assert.False(prerequisite.Passed);
        Assert.Contains("SEC-220", prerequisite.Detail);
    }

    [Fact]
    public void Evaluate_WhenCourseRequiresApproval_PreservesApprovalRequirement()
    {
        var result = _rules.Evaluate(_learner, Course(requiresApproval: true), []);

        Assert.True(result.Eligible);
        Assert.True(result.RequiresApproval);
    }

    private static Course Course(
        bool isActive = true,
        int capacity = 10,
        bool requiresApproval = false,
        IReadOnlyList<string>? prerequisites = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = "TEST-101",
            Title = "Test Course",
            IsActive = isActive,
            Capacity = capacity,
            RequiresManagerApproval = requiresApproval,
            PrerequisiteCodes = prerequisites ?? ["PREREQ-101"]
        };
}
