using LearnFlow.Application.Abstractions;
using LearnFlow.Domain.Entities;

namespace LearnFlow.Infrastructure.Repositories;

public sealed class InMemoryLearningRepository : ILearningRepository
{
    private readonly List<Course> _courses = SeedCourses();
    private readonly List<Learner> _learners = SeedLearners();
    private readonly List<EnrollmentRequest> _enrollments;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public InMemoryLearningRepository()
    {
        _enrollments = SeedEnrollments(_courses, _learners);
    }

    public Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Course>>(_courses.ToArray());

    public Task<Course?> GetCourseAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_courses.SingleOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<Learner>> GetLearnersAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Learner>>(_learners.ToArray());

    public Task<Learner?> GetLearnerAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_learners.SingleOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<EnrollmentRequest>> GetEnrollmentsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EnrollmentRequest>>(_enrollments.ToArray());

    public Task<EnrollmentRequest?> GetEnrollmentAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_enrollments.SingleOrDefault(item => item.Id == id));

    public async Task AddEnrollmentAsync(
        EnrollmentRequest enrollment,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _enrollments.Add(enrollment);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    private static List<Course> SeedCourses() =>
    [
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Code = "NET-801",
            Title = "Secure APIs with .NET 8",
            Description = "Build maintainable REST APIs with validation, authentication, and structured error handling.",
            Category = "Software Development",
            Instructor = "Dana Brooks",
            DurationHours = 12,
            Capacity = 24,
            RequiresManagerApproval = false,
            Accent = "blue",
            PrerequisiteCodes = ["CSHARP-201"]
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Code = "BPM-810",
            Title = "Process Automation with Camunda",
            Description = "Model BPMN workflows, implement service tasks, and handle human approvals and failures.",
            Category = "Workflow Automation",
            Instructor = "Marcus Reed",
            DurationHours = 16,
            Capacity = 18,
            RequiresManagerApproval = true,
            Accent = "violet"
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            Code = "TST-420",
            Title = "Unit Testing Business Rules",
            Description = "Use xUnit and Moq to prove that application behavior meets defined business requirements.",
            Category = "Software Quality",
            Instructor = "Elena Ruiz",
            DurationHours = 8,
            Capacity = 30,
            RequiresManagerApproval = false,
            Accent = "green"
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            Code = "SQL-330",
            Title = "SQL Performance Fundamentals",
            Description = "Diagnose slow queries and improve stored procedures, indexes, and data-access patterns.",
            Category = "Data",
            Instructor = "Jordan Patel",
            DurationHours = 10,
            Capacity = 20,
            RequiresManagerApproval = false,
            Accent = "amber"
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
            Code = "WEB-510",
            Title = "React and TypeScript Refresher",
            Description = "Build typed components, manage state, integrate APIs, and test common UI behavior.",
            Category = "Web Development",
            Instructor = "Nia Foster",
            DurationHours = 14,
            Capacity = 22,
            RequiresManagerApproval = true,
            Accent = "coral"
        },
        new()
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
            Code = "OPS-610",
            Title = "Production Support and Observability",
            Description = "Use logs, traces, metrics, and runbooks to diagnose and prevent recurring failures.",
            Category = "Application Reliability",
            Instructor = "Chris Morgan",
            DurationHours = 9,
            Capacity = 20,
            RequiresManagerApproval = false,
            Accent = "cyan"
        }
    ];

    private static List<Learner> SeedLearners() =>
    [
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Name = "Maya Chen",
            Email = "maya.chen@learnflow.local",
            Department = "Application Engineering",
            CompletedCourseCodes = ["CSHARP-201", "SQL-210"]
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            Name = "Andre Williams",
            Email = "andre.williams@learnflow.local",
            Department = "Business Systems",
            CompletedCourseCodes = ["BPM-201"]
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
            Name = "Priya Shah",
            Email = "priya.shah@learnflow.local",
            Department = "Quality Engineering",
            CompletedCourseCodes = ["CSHARP-201", "TST-210"]
        },
        new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
            Name = "Luis Martinez",
            Email = "luis.martinez@learnflow.local",
            Department = "Operations",
            CompletedCourseCodes = []
        }
    ];

    private static List<EnrollmentRequest> SeedEnrollments(
        IReadOnlyList<Course> courses,
        IReadOnlyList<Learner> learners)
    {
        EnrollmentRequest Create(int learner, int course, EnrollmentStatus status, int hoursAgo)
        {
            var enrollment = new EnrollmentRequest
            {
                Id = Guid.NewGuid(),
                LearnerId = learners[learner].Id,
                CourseId = courses[course].Id,
                Status = status,
                SubmittedAt = DateTimeOffset.UtcNow.AddHours(-hoursAgo),
                ProcessInstanceKey = status == EnrollmentStatus.AwaitingApproval
                    ? $"22517998136{hoursAgo:0000}"
                    : null
            };
            enrollment.RuleChecks =
            [
                new("Course is active", true, "Course is open for enrollment."),
                new("Enrollment is not duplicated", true, "No active duplicate was found."),
                new("Seat is available", true, "Seats remain."),
                new("Prerequisites are satisfied", true, "All required courses are complete.")
            ];
            enrollment.History.Add(new(enrollment.SubmittedAt, "request.submitted", "Enrollment request submitted."));
            enrollment.History.Add(new(enrollment.SubmittedAt.AddMinutes(1), "rules.passed", "Business requirements were satisfied."));
            if (status == EnrollmentStatus.Enrolled)
                enrollment.History.Add(new(enrollment.SubmittedAt.AddMinutes(3), "enrollment.completed", "Learner was enrolled."));
            return enrollment;
        }

        return
        [
            Create(0, 2, EnrollmentStatus.Enrolled, 2),
            Create(1, 1, EnrollmentStatus.AwaitingApproval, 5),
            Create(2, 3, EnrollmentStatus.Enrolled, 18),
            Create(3, 5, EnrollmentStatus.Enrolled, 27),
            Create(0, 4, EnrollmentStatus.AwaitingApproval, 30)
        ];
    }
}
