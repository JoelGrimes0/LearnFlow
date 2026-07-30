export type RuleCheck = {
  rule: string;
  passed: boolean;
  detail: string;
};

export type EnrollmentEvent = {
  occurredAt: string;
  type: string;
  message: string;
};

export type Course = {
  id: string;
  code: string;
  title: string;
  description: string;
  category: string;
  instructor: string;
  durationHours: number;
  capacity: number;
  enrolled: number;
  isActive: boolean;
  requiresManagerApproval: boolean;
  accent: string;
  prerequisiteCodes: string[];
};

export type Learner = {
  id: string;
  name: string;
  email: string;
  department: string;
  completedCourseCodes: string[];
};

export type Enrollment = {
  id: string;
  learnerId: string;
  learner: string;
  department: string;
  courseId: string;
  courseCode: string;
  course: string;
  status: string;
  submittedAt: string;
  processInstanceKey: string | null;
  ruleChecks: RuleCheck[];
  history: EnrollmentEvent[];
};

export type Dashboard = {
  activeCourses: number;
  totalLearners: number;
  enrolledLearners: number;
  pendingApprovals: number;
  completionRate: number;
  recentEnrollments: Enrollment[];
};

export type Health = {
  status: string;
  workflowMode: "camunda" | "demo";
  timestamp: string;
};
