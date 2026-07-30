import type { Course, Dashboard, Enrollment, Health, Learner } from "../types";

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as
      | { error?: string; detail?: string }
      | null;
    throw new Error(body?.error ?? body?.detail ?? `Request failed (${response.status}).`);
  }

  return response.json() as Promise<T>;
}

export const api = {
  health: () => request<Health>("/api/health"),
  dashboard: () => request<Dashboard>("/api/dashboard"),
  courses: () => request<Course[]>("/api/courses"),
  learners: () => request<Learner[]>("/api/learners"),
  enrollments: () => request<Enrollment[]>("/api/enrollments"),
  submitEnrollment: (learnerId: string, courseId: string) =>
    request<Enrollment>("/api/enrollments", {
      method: "POST",
      body: JSON.stringify({ learnerId, courseId }),
    }),
  reviewEnrollment: (id: string, approved: boolean, reviewer: string) =>
    request<Enrollment>(`/api/enrollments/${id}/approval`, {
      method: "POST",
      body: JSON.stringify({ approved, reviewer }),
    }),
};
