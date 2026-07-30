import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "./api";

const fetchMock = vi.fn();

beforeEach(() => {
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => {
  fetchMock.mockReset();
  vi.unstubAllGlobals();
});

describe("api", () => {
  it("loads health data with JSON headers", async () => {
    const health = {
      status: "healthy",
      workflowMode: "demo" as const,
      timestamp: "2026-07-30T18:49:03Z",
    };
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify(health), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await expect(api.health()).resolves.toEqual(health);
    expect(fetchMock).toHaveBeenCalledWith("/api/health", {
      headers: { "Content-Type": "application/json" },
    });
  });

  it("serializes a new enrollment request", async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ id: "enrollment-1" }), {
        status: 201,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await api.submitEnrollment("learner-1", "course-1");

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/enrollments",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          learnerId: "learner-1",
          courseId: "course-1",
        }),
      }),
    );
  });

  it("serializes an enrollment review", async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ id: "enrollment-1" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await api.reviewEnrollment("enrollment-1", true, "Joel Grimes");

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/enrollments/enrollment-1/approval",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({
          approved: true,
          reviewer: "Joel Grimes",
        }),
      }),
    );
  });

  it.each([
    [{ error: "The course is full." }, "The course is full."],
    [{ detail: "Camunda is unavailable." }, "Camunda is unavailable."],
  ])("uses the API error message from %o", async (body, expectedMessage) => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify(body), {
        status: 400,
        headers: { "Content-Type": "application/json" },
      }),
    );

    await expect(api.courses()).rejects.toThrow(expectedMessage);
  });

  it("falls back to the response status when an error is not JSON", async () => {
    fetchMock.mockResolvedValue(new Response("Unavailable", { status: 503 }));

    await expect(api.dashboard()).rejects.toThrow("Request failed (503).");
  });
});
