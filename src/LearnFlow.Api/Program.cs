using LearnFlow.Application.Abstractions;
using LearnFlow.Application.Contracts;
using LearnFlow.Application.Services;
using LearnFlow.Domain.Rules;
using LearnFlow.Infrastructure.Camunda;
using LearnFlow.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddSingleton<ILearningRepository, InMemoryLearningRepository>();
builder.Services.AddSingleton<IEnrollmentRulesService, EnrollmentRulesService>();
builder.Services.AddSingleton<EnrollmentWorkflowProcessor>();
builder.Services.AddSingleton<LearningService>();

var camundaOptions = builder.Configuration
    .GetSection(CamundaOptions.SectionName)
    .Get<CamundaOptions>() ?? new CamundaOptions();
builder.Services.AddSingleton(camundaOptions);

if (camundaOptions.Enabled)
{
    builder.Services.AddHttpClient<CamundaRestClient>(client =>
        client.BaseAddress = new Uri(camundaOptions.BaseUrl));
    builder.Services.AddSingleton<IEnrollmentWorkflowOrchestrator, CamundaEnrollmentOrchestrator>();
    builder.Services.AddHostedService<CamundaDeploymentService>();
    builder.Services.AddHostedService<CamundaJobWorker>();
}
else
{
    builder.Services.AddSingleton<IEnrollmentWorkflowOrchestrator, DemoEnrollmentOrchestrator>();
}

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    workflowMode = camundaOptions.Enabled ? "camunda" : "demo",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/dashboard", async (LearningService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetDashboardAsync(cancellationToken)));

app.MapGet("/api/courses", async (LearningService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetCoursesAsync(cancellationToken)));

app.MapGet("/api/learners", async (LearningService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetLearnersAsync(cancellationToken)));

app.MapGet("/api/enrollments", async (LearningService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetEnrollmentsAsync(cancellationToken)));

app.MapGet("/api/enrollments/{id:guid}", async (
    Guid id,
    LearningService service,
    CancellationToken cancellationToken) =>
{
    var enrollment = await service.GetEnrollmentAsync(id, cancellationToken);
    return enrollment is null ? Results.NotFound() : Results.Ok(enrollment);
});

app.MapPost("/api/enrollments", async (
    SubmitEnrollmentRequest request,
    LearningService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var enrollment = await service.SubmitAsync(request, cancellationToken);
        return Results.Created($"/api/enrollments/{enrollment.Id}", enrollment);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (HttpRequestException exception)
    {
        return Results.Problem(
            title: "Camunda workflow could not be started.",
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPost("/api/enrollments/{id:guid}/approval", async (
    Guid id,
    ApprovalRequest request,
    LearningService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await service.ApproveAsync(id, request, cancellationToken));
    }
    catch (KeyNotFoundException exception)
    {
        return Results.NotFound(new { error = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.Run();

public partial class Program
{
}
