import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  ArrowRight,
  BookOpen,
  Check,
  CheckCircle2,
  ChevronRight,
  CircleAlert,
  ClipboardCheck,
  Clock3,
  Code2,
  FileCheck2,
  GraduationCap,
  LayoutDashboard,
  RefreshCw,
  Search,
  ShieldCheck,
  UserCheck,
  Users,
  Workflow,
  X,
  XCircle,
} from "lucide-react";
import { api } from "./lib/api";
import type { Course, Dashboard, Enrollment, Health, Learner } from "./types";

type View = "dashboard" | "catalog" | "enrollments" | "rules";

const navigation: { id: View; label: string; icon: typeof LayoutDashboard }[] = [
  { id: "dashboard", label: "Overview", icon: LayoutDashboard },
  { id: "catalog", label: "Course catalog", icon: BookOpen },
  { id: "enrollments", label: "Enrollments", icon: ClipboardCheck },
  { id: "rules", label: "Rules & tests", icon: FileCheck2 },
];

const statusClass = (status: string) => status.replace(/([a-z])([A-Z])/g, "$1-$2").toLowerCase();
const prettyStatus = (status: string) => status.replace(/([a-z])([A-Z])/g, "$1 $2");

function App() {
  const [view, setView] = useState<View>("dashboard");
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [courses, setCourses] = useState<Course[]>([]);
  const [learners, setLearners] = useState<Learner[]>([]);
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [health, setHealth] = useState<Health | null>(null);
  const [selected, setSelected] = useState<Enrollment | null>(null);
  const [showRequest, setShowRequest] = useState(false);
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setBusy(true);
    try {
      const [dashboardData, courseData, learnerData, enrollmentData, healthData] =
        await Promise.all([
          api.dashboard(),
          api.courses(),
          api.learners(),
          api.enrollments(),
          api.health(),
        ]);
      setDashboard(dashboardData);
      setCourses(courseData);
      setLearners(learnerData);
      setEnrollments(enrollmentData);
      setHealth(healthData);
      setError(null);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "The API could not be reached.");
    } finally {
      setBusy(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark"><GraduationCap size={24} /></div>
          <div><strong>LearnFlow</strong><span>Learning operations</span></div>
        </div>
        <nav aria-label="Primary navigation">
          {navigation.map((item) => {
            const Icon = item.icon;
            return (
              <button
                className={view === item.id ? "nav-item active" : "nav-item"}
                key={item.id}
                onClick={() => setView(item.id)}
              >
                <Icon size={19} />
                <span>{item.label}</span>
              </button>
            );
          })}
        </nav>
        <div className="workflow-card">
          <div className="workflow-icon"><Workflow size={20} /></div>
          <strong>Workflow engine</strong>
          <span>{health?.workflowMode === "camunda" ? "Camunda 8 connected" : "Local demo mode"}</span>
          <div className="connection"><i /> Operational</div>
        </div>
        <div className="profile">
          <div className="avatar">JG</div>
          <div><strong>Joel Grimes</strong><span>LMS Administrator</span></div>
        </div>
      </aside>

      <main>
        <header className="topbar">
          <div>
            <p className="eyebrow">LEARNING MANAGEMENT</p>
            <h1>{navigation.find((item) => item.id === view)?.label}</h1>
          </div>
          <div className="topbar-actions">
            <button className="icon-button" aria-label="Refresh data" onClick={() => void load()}>
              <RefreshCw size={18} className={busy ? "spin" : ""} />
            </button>
            <button className="primary-button" onClick={() => setShowRequest(true)}>
              <span>New enrollment</span><ArrowRight size={17} />
            </button>
          </div>
        </header>

        {error && (
          <div className="error-banner">
            <CircleAlert size={20} />
            <div><strong>LearnFlow API is unavailable.</strong><span>{error}</span></div>
            <button onClick={() => void load()}>Try again</button>
          </div>
        )}

        {!error && busy && !dashboard ? (
          <div className="loading"><RefreshCw size={28} className="spin" /> Loading learning data...</div>
        ) : (
          <>
            {view === "dashboard" && dashboard && (
              <DashboardView
                dashboard={dashboard}
                courses={courses}
                onSelectEnrollment={(enrollment) => setSelected(enrollment)}
                onOpenCatalog={() => setView("catalog")}
              />
            )}
            {view === "catalog" && (
              <CatalogView courses={courses} onEnroll={() => setShowRequest(true)} />
            )}
            {view === "enrollments" && (
              <EnrollmentsView
                enrollments={enrollments}
                onSelect={(enrollment) => setSelected(enrollment)}
              />
            )}
            {view === "rules" && <RulesView />}
          </>
        )}
      </main>

      {showRequest && (
        <EnrollmentModal
          courses={courses}
          learners={learners}
          onClose={() => setShowRequest(false)}
          onSubmitted={async () => {
            setShowRequest(false);
            await load();
            setView("enrollments");
          }}
        />
      )}
      {selected && (
        <EnrollmentDrawer
          enrollment={selected}
          onClose={() => setSelected(null)}
          onReviewed={async () => {
            setSelected(null);
            await load();
          }}
        />
      )}
    </div>
  );
}

function DashboardView({
  dashboard,
  courses,
  onSelectEnrollment,
  onOpenCatalog,
}: {
  dashboard: Dashboard;
  courses: Course[];
  onSelectEnrollment: (enrollment: Enrollment) => void;
  onOpenCatalog: () => void;
}) {
  const metrics = [
    { label: "Active courses", value: dashboard.activeCourses, icon: BookOpen, tone: "blue" },
    { label: "Learners", value: dashboard.totalLearners, icon: Users, tone: "violet" },
    { label: "Current enrollments", value: dashboard.enrolledLearners, icon: GraduationCap, tone: "green" },
    { label: "Pending approvals", value: dashboard.pendingApprovals, icon: Clock3, tone: "amber" },
  ];

  return (
    <div className="page-content">
      <section className="welcome">
        <div>
          <span className="section-kicker">Thursday, July 30</span>
          <h2>Good afternoon, Joel.</h2>
          <p>Here is what is happening across your learning programs and enrollment workflow.</p>
        </div>
        <div className="completion-ring" style={{ "--progress": `${dashboard.completionRate * 3.6}deg` } as React.CSSProperties}>
          <div><strong>{dashboard.completionRate}%</strong><span>approval yield</span></div>
        </div>
      </section>

      <section className="metric-grid">
        {metrics.map((metric) => {
          const Icon = metric.icon;
          return (
            <article className="metric-card" key={metric.label}>
              <div className={`metric-icon ${metric.tone}`}><Icon size={20} /></div>
              <span>{metric.label}</span>
              <strong>{metric.value}</strong>
              <small>Live learning data</small>
            </article>
          );
        })}
      </section>

      <section className="dashboard-grid">
        <article className="panel recent-panel">
          <div className="panel-heading">
            <div><span className="section-kicker">WORKFLOW ACTIVITY</span><h3>Recent enrollment requests</h3></div>
            <Activity size={20} />
          </div>
          <div className="request-list">
            {dashboard.recentEnrollments.map((enrollment) => (
              <button key={enrollment.id} onClick={() => onSelectEnrollment(enrollment)}>
                <span className={`course-dot ${statusClass(enrollment.status)}`} />
                <div><strong>{enrollment.learner}</strong><span>{enrollment.course}</span></div>
                <span className={`status ${statusClass(enrollment.status)}`}>{prettyStatus(enrollment.status)}</span>
                <ChevronRight size={17} />
              </button>
            ))}
          </div>
        </article>

        <article className="panel featured-panel">
          <div className="panel-heading">
            <div><span className="section-kicker">COURSE CATALOG</span><h3>Popular this month</h3></div>
            <button className="text-button" onClick={onOpenCatalog}>View catalog <ArrowRight size={15} /></button>
          </div>
          {courses.slice(0, 3).map((course) => (
            <div className="featured-course" key={course.id}>
              <div className={`course-emblem ${course.accent}`}><Code2 size={20} /></div>
              <div><strong>{course.title}</strong><span>{course.code} · {course.durationHours} hours</span></div>
              <div className="seat-count"><strong>{course.capacity - course.enrolled}</strong><span>seats</span></div>
            </div>
          ))}
        </article>
      </section>

      <WorkflowSummary />
    </div>
  );
}

function WorkflowSummary() {
  const steps = [
    { label: "Request submitted", icon: ClipboardCheck },
    { label: "Rules evaluated", icon: ShieldCheck },
    { label: "Manager approval", icon: UserCheck },
    { label: "Learner enrolled", icon: GraduationCap },
  ];
  return (
    <section className="panel workflow-summary">
      <div className="panel-heading">
        <div><span className="section-kicker">CAMUNDA 8 PROCESS</span><h3>Course enrollment workflow</h3></div>
        <span className="workflow-version"><i /> BPMN v1.0</span>
      </div>
      <div className="workflow-steps">
        {steps.map((step, index) => {
          const Icon = step.icon;
          return (
            <div className="workflow-step" key={step.label}>
              <div className="step-icon"><Icon size={21} /></div>
              <div><small>STEP {index + 1}</small><strong>{step.label}</strong></div>
              {index < steps.length - 1 && <ArrowRight className="step-arrow" size={20} />}
            </div>
          );
        })}
      </div>
    </section>
  );
}

function CatalogView({ courses, onEnroll }: { courses: Course[]; onEnroll: () => void }) {
  const [query, setQuery] = useState("");
  const filtered = useMemo(
    () => courses.filter((course) =>
      `${course.title} ${course.code} ${course.category}`.toLowerCase().includes(query.toLowerCase())),
    [courses, query],
  );
  return (
    <div className="page-content">
      <section className="catalog-intro">
        <div><span className="section-kicker">BUILD CAPABILITY</span><h2>Find the next course</h2><p>Technical learning designed around practical business outcomes.</p></div>
        <label className="search-box"><Search size={18} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search courses" /></label>
      </section>
      <section className="course-grid">
        {filtered.map((course) => (
          <article className="course-card" key={course.id}>
            <div className={`course-banner ${course.accent}`}><span>{course.category}</span><BookOpen size={26} /></div>
            <div className="course-body">
              <span className="course-code">{course.code}</span>
              <h3>{course.title}</h3>
              <p>{course.description}</p>
              <div className="course-meta"><span><Clock3 size={15} /> {course.durationHours} hours</span><span><Users size={15} /> {course.capacity - course.enrolled} seats</span></div>
              <div className="course-footer"><div><small>Instructor</small><strong>{course.instructor}</strong></div><button onClick={onEnroll}>Request seat</button></div>
            </div>
          </article>
        ))}
      </section>
    </div>
  );
}

function EnrollmentsView({ enrollments, onSelect }: { enrollments: Enrollment[]; onSelect: (item: Enrollment) => void }) {
  const [filter, setFilter] = useState("All");
  const statuses = ["All", "AwaitingApproval", "Enrolled", "Rejected"];
  const filtered = filter === "All" ? enrollments : enrollments.filter((item) => item.status === filter);
  return (
    <div className="page-content">
      <section className="list-header">
        <div><span className="section-kicker">REQUEST MANAGEMENT</span><h2>Enrollment queue</h2><p>Review the current state and rule results for every request.</p></div>
        <div className="filter-tabs">{statuses.map((status) => <button className={filter === status ? "active" : ""} onClick={() => setFilter(status)} key={status}>{prettyStatus(status)}</button>)}</div>
      </section>
      <section className="panel table-panel">
        <table>
          <thead><tr><th>Learner</th><th>Course</th><th>Submitted</th><th>Rules</th><th>Status</th><th /></tr></thead>
          <tbody>
            {filtered.map((item) => (
              <tr key={item.id} onClick={() => onSelect(item)}>
                <td><strong>{item.learner}</strong><span>{item.department}</span></td>
                <td><strong>{item.course}</strong><span>{item.courseCode}</span></td>
                <td>{new Date(item.submittedAt).toLocaleDateString()}</td>
                <td><span className="rules-result">{item.ruleChecks.filter((rule) => rule.passed).length}/{item.ruleChecks.length || 4} passed</span></td>
                <td><span className={`status ${statusClass(item.status)}`}>{prettyStatus(item.status)}</span></td>
                <td><ChevronRight size={17} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}

function RulesView() {
  const rules = [
    { name: "Course must be active", test: "Evaluate_WhenCourseIsInactive_RejectsEnrollment", icon: BookOpen },
    { name: "No duplicate enrollment", test: "Evaluate_WhenActiveDuplicateExists_RejectsEnrollment", icon: ClipboardCheck },
    { name: "Course must have capacity", test: "Evaluate_WhenCourseIsFull_RejectsEnrollment", icon: Users },
    { name: "Prerequisites must be complete", test: "Evaluate_WhenPrerequisiteIsMissing_ExplainsMissingCourse", icon: ShieldCheck },
  ];
  return (
    <div className="page-content">
      <section className="rules-hero">
        <div><span className="section-kicker">BUSINESS REQUIREMENT VERIFICATION</span><h2>Rules backed by automated tests</h2><p>Every enrollment rule has an explicit xUnit test that proves the application behaves as the business expects.</p></div>
        <div className="test-score"><CheckCircle2 size={30} /><strong>8 / 8</strong><span>domain tests passing</span></div>
      </section>
      <section className="rules-layout">
        <article className="panel rules-panel">
          <div className="panel-heading"><div><span className="section-kicker">RULE CATALOG</span><h3>Enrollment requirements</h3></div><ShieldCheck size={20} /></div>
          {rules.map((rule) => {
            const Icon = rule.icon;
            return (
              <div className="rule-row" key={rule.name}>
                <div className="rule-icon"><Icon size={18} /></div>
                <div><strong>{rule.name}</strong><code>{rule.test}</code></div>
                <span><Check size={15} /> Covered</span>
              </div>
            );
          })}
        </article>
        <article className="panel code-panel">
          <div className="code-heading"><span>EnrollmentRulesServiceTests.cs</span><i>● ● ●</i></div>
          <pre><code><span className="kw">[Fact]</span>{"\n"}<span className="kw">public void</span> CourseAtCapacity_RejectsEnrollment(){"\n"}{"{"}{"\n"}  <span className="kw">var</span> decision = rules.Evaluate(learner, course, existing);{"\n\n"}  Assert.<span className="method">False</span>(decision.Eligible);{"\n"}  Assert.<span className="method">Contains</span>(decision.Checks, check ={">"}{"\n"}    check.Rule == <span className="string">"Seat is available"</span> &amp;&amp; !check.Passed);{"\n"}{"}"}</code></pre>
          <div className="code-note"><FileCheck2 size={18} /><div><strong>Why this matters</strong><span>The test verifies a business outcome—not an implementation detail.</span></div></div>
        </article>
      </section>
    </div>
  );
}

function EnrollmentModal({ courses, learners, onClose, onSubmitted }: { courses: Course[]; learners: Learner[]; onClose: () => void; onSubmitted: () => Promise<void> }) {
  const [learnerId, setLearnerId] = useState(learners[0]?.id ?? "");
  const [courseId, setCourseId] = useState(courses[0]?.id ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    try {
      await api.submitEnrollment(learnerId, courseId);
      await onSubmitted();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Enrollment could not be submitted.");
      setSubmitting(false);
    }
  };
  return (
    <div className="modal-backdrop" role="presentation">
      <form className="modal" onSubmit={(event) => void submit(event)}>
        <div className="modal-heading"><div><span className="section-kicker">NEW REQUEST</span><h2>Request course enrollment</h2></div><button type="button" onClick={onClose} aria-label="Close"><X /></button></div>
        <p>Camunda will coordinate rule validation, manager approval when required, enrollment, and notification.</p>
        <label>Learner<select value={learnerId} onChange={(event) => setLearnerId(event.target.value)}>{learners.map((learner) => <option key={learner.id} value={learner.id}>{learner.name} — {learner.department}</option>)}</select></label>
        <label>Course<select value={courseId} onChange={(event) => setCourseId(event.target.value)}>{courses.map((course) => <option key={course.id} value={course.id}>{course.code} — {course.title}</option>)}</select></label>
        <div className="process-preview"><Workflow size={20} /><span>Submit</span><ArrowRight size={14} /><span>Rules</span><ArrowRight size={14} /><span>Approval</span><ArrowRight size={14} /><span>Enroll</span></div>
        {error && <div className="form-error">{error}</div>}
        <div className="modal-actions"><button type="button" className="secondary-button" onClick={onClose}>Cancel</button><button className="primary-button" disabled={submitting}>{submitting ? "Submitting..." : "Start workflow"}<ArrowRight size={16} /></button></div>
      </form>
    </div>
  );
}

function EnrollmentDrawer({ enrollment, onClose, onReviewed }: { enrollment: Enrollment; onClose: () => void; onReviewed: () => Promise<void> }) {
  const [busy, setBusy] = useState(false);
  const review = async (approved: boolean) => {
    setBusy(true);
    await api.reviewEnrollment(enrollment.id, approved, "Joel Grimes");
    await onReviewed();
  };
  const steps = [
    { label: "Submitted", complete: true },
    { label: "Rules checked", complete: enrollment.ruleChecks.length > 0 },
    { label: "Manager approval", complete: ["Approved", "Enrolled"].includes(enrollment.status), current: enrollment.status === "AwaitingApproval" },
    { label: "Enrolled", complete: enrollment.status === "Enrolled" },
  ];
  return (
    <div className="drawer-backdrop" role="presentation">
      <aside className="drawer">
        <div className="drawer-heading"><div><span className="section-kicker">{enrollment.courseCode}</span><h2>{enrollment.learner}</h2><p>{enrollment.course}</p></div><button onClick={onClose} aria-label="Close"><X /></button></div>
        <span className={`status large ${statusClass(enrollment.status)}`}>{prettyStatus(enrollment.status)}</span>
        <div className="drawer-section"><h3>Workflow progress</h3><div className="vertical-workflow">{steps.map((step) => <div className={step.current ? "current" : step.complete ? "complete" : ""} key={step.label}><i>{step.complete ? <Check size={14} /> : ""}</i><span>{step.label}</span></div>)}</div></div>
        <div className="drawer-section"><h3>Business-rule results</h3>{enrollment.ruleChecks.length === 0 ? <p className="muted">Waiting for the rules worker.</p> : enrollment.ruleChecks.map((rule) => <div className="check-row" key={rule.rule}>{rule.passed ? <CheckCircle2 className="pass" size={19} /> : <XCircle className="fail" size={19} />}<div><strong>{rule.rule}</strong><span>{rule.detail}</span></div></div>)}</div>
        <div className="drawer-section"><h3>Audit history</h3>{enrollment.history.map((event) => <div className="history-row" key={`${event.occurredAt}-${event.type}`}><time>{new Date(event.occurredAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</time><div><strong>{event.type}</strong><span>{event.message}</span></div></div>)}</div>
        {enrollment.status === "AwaitingApproval" && <div className="approval-actions"><button disabled={busy} onClick={() => void review(false)}>Deny</button><button disabled={busy} onClick={() => void review(true)}><UserCheck size={17} /> Approve enrollment</button></div>}
      </aside>
    </div>
  );
}

export default App;
