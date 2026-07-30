import { cleanup, render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import App from "./App";
import { api } from "./lib/api";
import type { Course, Dashboard, Enrollment, Health, Learner } from "./types";

vi.mock("./lib/api", () => ({
  api: {
    health: vi.fn(),
    dashboard: vi.fn(),
    courses: vi.fn(),
    learners: vi.fn(),
    enrollments: vi.fn(),
    submitEnrollment: vi.fn(),
    reviewEnrollment: vi.fn(),
  },
}));

const courses: Course[] = [
  {
    id: "course-1",
    code: "SEC-101",
    title: "Security Fundamentals",
    description: "Learn practical security controls.",
    category: "Security",
    instructor: "Avery Stone",
    durationHours: 8,
    capacity: 20,
    enrolled: 12,
    isActive: true,
    requiresManagerApproval: false,
    accent: "blue",
    prerequisiteCodes: [],
  },
  {
    id: "course-2",
    code: "LDR-201",
    title: "Leadership Essentials",
    description: "Build effective leadership habits.",
    category: "Leadership",
    instructor: "Morgan Lee",
    durationHours: 6,
    capacity: 16,
    enrolled: 9,
    isActive: true,
    requiresManagerApproval: true,
    accent: "violet",
    prerequisiteCodes: [],
  },
];

const learners: Learner[] = [
  {
    id: "learner-1",
    name: "Taylor Morgan",
    email: "taylor@example.com",
    department: "Engineering",
    completedCourseCodes: [],
  },
];

const enrollment: Enrollment = {
  id: "enrollment-1",
  learnerId: "learner-1",
  learner: "Taylor Morgan",
  department: "Engineering",
  courseId: "course-1",
  courseCode: "SEC-101",
  course: "Security Fundamentals",
  status: "Enrolled",
  submittedAt: "2026-07-30T12:00:00Z",
  processInstanceKey: null,
  ruleChecks: [],
  history: [],
};

const dashboard: Dashboard = {
  activeCourses: 2,
  totalLearners: 1,
  enrolledLearners: 1,
  pendingApprovals: 0,
  completionRate: 100,
  recentEnrollments: [enrollment],
};

const health: Health = {
  status: "healthy",
  workflowMode: "demo",
  timestamp: "2026-07-30T12:00:00Z",
};

const mockedApi = vi.mocked(api, { deep: true });

beforeEach(() => {
  vi.clearAllMocks();
  mockedApi.dashboard.mockResolvedValue(dashboard);
  mockedApi.courses.mockResolvedValue(courses);
  mockedApi.learners.mockResolvedValue(learners);
  mockedApi.enrollments.mockResolvedValue([enrollment]);
  mockedApi.health.mockResolvedValue(health);
  mockedApi.submitEnrollment.mockResolvedValue(enrollment);
});

afterEach(() => {
  cleanup();
});

describe("App", () => {
  it("loads and displays the learning dashboard", async () => {
    render(<App />);

    expect(await screen.findByText("Good afternoon, Joel.")).toBeTruthy();
    expect(screen.getByText("Local demo mode")).toBeTruthy();
    expect(screen.getAllByText("Security Fundamentals")).toHaveLength(2);
    expect(mockedApi.dashboard).toHaveBeenCalledOnce();
    expect(mockedApi.health).toHaveBeenCalledOnce();
  });

  it("filters the course catalog by the search query", async () => {
    const user = userEvent.setup();
    render(<App />);
    await screen.findByText("Good afternoon, Joel.");

    await user.click(screen.getByRole("button", { name: "Course catalog" }));
    const search = screen.getByPlaceholderText("Search courses");
    await user.type(search, "security");

    expect(screen.getByText("Security Fundamentals")).toBeTruthy();
    expect(screen.queryByText("Leadership Essentials")).toBeNull();
  });

  it("submits a new enrollment using the selected learner and course", async () => {
    const user = userEvent.setup();
    render(<App />);
    await screen.findByText("Good afternoon, Joel.");

    await user.click(screen.getByRole("button", { name: /new enrollment/i }));
    const modal = screen.getByRole("heading", {
      name: "Request course enrollment",
    }).closest("form");
    expect(modal).not.toBeNull();

    await user.click(
      within(modal as HTMLFormElement).getByRole("button", {
        name: /start workflow/i,
      }),
    );

    await waitFor(() => {
      expect(mockedApi.submitEnrollment).toHaveBeenCalledWith(
        "learner-1",
        "course-1",
      );
    });
  });
});
