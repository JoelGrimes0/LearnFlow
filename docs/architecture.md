# LearnFlow architecture

LearnFlow separates business behavior from process orchestration:

```mermaid
flowchart LR
    UI[React + TypeScript] --> API[ASP.NET Core API]
    API --> APP[Application services]
    APP --> RULES[Enrollment rules]
    APP --> REPO[Learning repository]
    API --> CAM[Camunda 8 REST API]
    CAM --> WORKER[.NET job worker]
    WORKER --> APP
```

## Responsibilities

| Component | Responsibility |
| --- | --- |
| `LearnFlow.Domain` | Course, learner, enrollment, and testable business rules |
| `LearnFlow.Application` | Use cases, API contracts, and workflow-task processing |
| `LearnFlow.Infrastructure` | Camunda REST client, workers, deployment, and repository |
| `LearnFlow.Api` | HTTP endpoints, dependency injection, configuration, and Swagger |
| `client` | Learning dashboard, catalog, requests, approvals, and rule visibility |
| `process` | Executable Camunda 8 BPMN model |
| `tests` | xUnit and Moq verification of business behavior |

## Enrollment sequence

```mermaid
sequenceDiagram
    participant User
    participant API
    participant Camunda
    participant Worker
    participant Rules

    User->>API: Submit enrollment
    API->>Camunda: Start process instance
    Camunda->>Worker: Activate validate-enrollment job
    Worker->>Rules: Evaluate requirements
    Rules-->>Worker: Eligibility and approval decision
    Worker-->>Camunda: Complete job with variables
    alt Manager approval required
        Camunda-->>User: Create approval task
        User->>API: Approve or deny
        API->>Camunda: Complete user task
    end
    Camunda->>Worker: Complete or reject enrollment
    Worker-->>Camunda: Complete service task
```

## Why the business rules are outside BPMN

Camunda determines *when* work happens and what path the process follows. The
C# domain service determines *whether* the enrollment satisfies business
requirements. This keeps the rules:

- independently testable with xUnit;
- reusable outside a single process model;
- easy to review with business stakeholders;
- isolated from transport and workflow-engine details.
