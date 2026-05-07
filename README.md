# DevSecOps Secure Application Pipeline

> **Course Project** — Cybersecurity | 10 Groups × 3 Students

Students design, build, secure, and attack a containerized/traditional web application while implementing a full DevSecOps pipeline — all on GitHub.

---

## Table of Contents

1. [Objective](#objective)
2. [Scope & Deliverables](#scope--deliverables)
3. [Application Requirements](#application-requirements)
4. [Weekly Plan](#weekly-plan)
5. [Grading Rubric](#grading-rubric)
   - [01 — Table of Contents (5 marks)](#01--table-of-contents-5-marks)
   - [02 — Architecture & Threat Model (5 marks)](#02--architecture--threat-model-5-marks)
   - [03 — GitHub Pipeline Implementation (10 marks)](#03--github-pipeline-implementation-10-marks)
   - [04 — Vulnerability Discovery Depth (20 marks)](#04--vulnerability-discovery-depth-20-marks)
   - [05 — Exploitation Quality (10 marks)](#05--exploitation-quality-10-marks)
   - [06 — Remediation Effectiveness (10 marks)](#06--remediation-effectiveness-10-marks)
   - [07 — Report Quality (5 marks)](#07--report-quality-5-marks)
   - [08 — GitHub Issues, Pushes & Updates (5 marks)](#08--github-issues-pushes--updates-5-marks)
   - [09 — Member Contributions (5 marks)](#09--member-contributions-5-marks)
   - [10 — Presentation & Live Demo (25 marks)](#10--presentation--live-demo-25-marks)
6. [Group vs Individual Split](#group-vs-individual-split)
7. [Policies & Guidelines](#policies--guidelines)

---

## Objective

Design, build, secure, and attack a containerized (or traditional) web application while implementing a full DevSecOps pipeline. The project spans four weeks and culminates in a live demo and professional security report.

---

## Scope & Deliverables

- Dockerized environment and/or traditional web app hosted on GitHub, including installation documents, requirements, and a user manual
- CI/CD pipeline with:
  - **SAST** — Static Application Security Testing
  - **DAST** — Dynamic Application Security Testing
  - **SCA** — Software Composition Analysis
- Threat model
- Exploitation report
- Final remediation + re-test report
- Executive summary (written for a non-technical audience)

---

## Application Requirements

The application **must** be running on an IP address / hostname with **HTTPS** (not localhost). It must include:

| Requirement | Details |
|---|---|
| **RBAC** | Admin + non-admin roles; each role sees only what it should |
| **Session management** | Proper login / logout mechanism — not a session that lives forever |
| **Separate pages** | Admin, common, and restricted pages clearly separated |
| **CRUD operations** | Create, Read, Update, Delete — all four, actually working |
| **User interaction** | Forms and inputs that do something meaningful |
| **Data handling** | Store and retrieve from a connected database |
| **Input validation** | Basic validation on all user-facing inputs |
| **Search / Filter** | At least one search or filter feature |

**Suggested page structure:**

- Home / Dashboard
- Main Feature Page (core functionality)
- Login / Register

> Groups whose application does not meet this baseline will be **capped at Level 2** for criteria 04, 05, and 06, regardless of report quality.

---

## Weekly Plan

| Week | Focus | Tasks |
|---|---|---|
| **Week 1** | Design | Design app + threat model; define attack surface |
| **Week 2** | Build | Build app + Dockerize; integrate SAST / SCA |
| **Week 3** | Attack | Perform DAST + manual pentesting; exploit vulnerabilities |
| **Week 4** | Fix & Report | Fix issues; generate professional report; present findings |

---

## Grading Rubric

Each criterion is scored across four levels:

| Level | Score | Description |
|---|---|---|
| 1 | 25% | Insufficient — bare minimum attempted, largely incomplete or copied |
| 2 | 50% | Developing — core requirements partially met, notable gaps |
| 3 | 75% | Proficient — requirements mostly met, minor gaps or polish issues |
| 4 | 100% | Exemplary — all requirements fully met, professional quality |

---

### 01 — Table of Contents (5 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 1.25 | TOC present but missing most sections; no page numbers or links; section titles don't match content |
| 2 | 2.5 | Most major sections listed; no hyperlinks; formatting inconsistent; subsections partially missing |
| 3 | 3.75 | All major sections present; subsections included; clickable links or page numbers provided; consistent formatting |
| 4 | 5 | Comprehensive TOC matching document exactly; all sections, subsections & appendices; functional navigation links; numbering consistent with report body; updated for final submission |

---

### 02 — Architecture & Threat Model (5 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 1.25 | Architecture is text-only; threat model is a generic template copy; no STRIDE applied; attack surface undefined |
| 2 | 2.5 | Basic architecture diagram present; threats listed but not categorised; partial mention of STRIDE/DREAD; attack surface vaguely described |
| 3 | 3.75 | Clear layered diagram (frontend / backend / DB); STRIDE applied to most components; trust boundaries identified; attack surface mapped to features |
| 4 | 5 | Professional DFD with actors, data flows & trust boundaries; full STRIDE per component with DREAD/CVSS scores; threat prioritisation matrix; attack surface tied to DAST/pentest scope; model updated post-remediation |

---

### 03 — GitHub Pipeline Implementation (10 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 2.5 | Repo exists but pipeline is minimal or broken; only one tool attempted; no CI triggers configured; config file incomplete |
| 2 | 5 | Pipeline runs at least two of SAST/DAST/SCA; some jobs pass; tools at default settings only; reports not consistently archived |
| 3 | 7.5 | All three stages integrated and running; triggers on push/PR events; reports stored as artifacts; app deployment step included |
| 4 | 10 | Full automated pipeline — SAST + DAST + SCA operational; fails on critical/high findings (quality gate); separate named jobs per stage; Dockerised app spun up for DAST in CI; remediation branch re-runs and passes |

---

### 04 — Vulnerability Discovery Depth (20 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 5 | Only tool output pasted without analysis; 1–2 findings described superficially; no OWASP/CVE references; high false-positive rate; no screenshots |
| 2 | 10 | Multiple findings from automated tools; basic description per vulnerability; some OWASP Top 10 mapping; severity mentioned but not justified; some screenshots |
| 3 | 15 | Good mix of automated + manual findings; all findings mapped to OWASP Top 10; CVSS scores calculated; screenshots / HTTP request evidence; false positives identified |
| 4 | 20 | Comprehensive findings including manual pentesting; business logic flaws (e.g. IDOR, broken RBAC); full OWASP Top 10 mapping with justifications; accurate CVSS v3.1 scores with vector strings; chained/multi-step vulnerabilities; minimal false positives |

---

### 05 — Exploitation Quality (10 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 2.5 | Exploitation claimed but no evidence or PoC; screenshots don't demonstrate impact; no reproduction steps; likely theoretical only |
| 2 | 5 | One vulnerability exploited with basic PoC; some screenshots; steps provided but incomplete; impact described at surface level |
| 3 | 7.5 | Multiple vulnerabilities exploited with clear PoC; step-by-step reproduction documented; impact demonstrated (data leakage, privilege escalation, etc.); attacker perspective articulated |
| 4 | 10 | Professional pentest-quality exploitation; chained exploits (e.g. XSS → session hijack → admin access); business impact quantified; full reproducible PoC with tool/script/payload; RBAC bypass or logic flaw specifically exploited |

---

### 06 — Remediation Effectiveness (10 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 2.5 | Issues acknowledged but no actual code fixes; remediation is generic advice only (e.g. "use HTTPS"); no re-test; pipeline still failing after "fixes" |
| 2 | 5 | Some code changes visible in GitHub; only low-severity issues fixed; re-test mentioned but not evidenced; root cause not addressed for major findings |
| 3 | 7.5 | Most critical/high findings remediated with commits; re-test screenshots / pipeline output provided; fix methodology explained; some regression considerations noted |
| 4 | 10 | All critical & high findings remediated at root cause level; pipeline re-run after fixes — passes quality gates; re-test confirms effectiveness per finding; regression checks or unit tests added; remaining low/informational risks formally accepted with rationale |

---

### 07 — Report Quality (5 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 1.25 | Poorly structured; major sections missing; grammar/spelling errors throughout; no executive summary; inconsistent formatting |
| 2 | 2.5 | Basic structure present and readable; executive summary included but too technical; inconsistent formatting; some findings lack evidence |
| 3 | 3.75 | Well-structured with clear section flow; executive summary readable by non-technical audience; consistent formatting; charts/tables used |
| 4 | 5 | Professional pentest-report quality; executive summary covers risk, business impact, and recommendations; findings table with severity/CVSS/status columns; annexes include raw tool output & screenshots; zero grammar issues |

---

### 08 — GitHub Issues, Pushes & Updates (5 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 1.25 | Fewer than 5 total commits across 4 weeks; no GitHub Issues created; no branching strategy; commit messages blank or meaningless |
| 2 | 2.5 | Some regular commits with basic messages; a few issues created but not linked; basic branch usage; activity clustered near deadline |
| 3 | 3.75 | Good commit history across all 4 weeks; issues linked in push messages per course guideline; feature/fix branches used per task; meaningful commit messages |
| 4 | 5 | Exemplary commit history — active across all 4 weeks; all pushes follow issue-association guideline; issues closed via commit references (`fixes #N`); milestones/labels used; pull requests used for merging with review comments |

---

### 09 — Member Contributions (5 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 1.25 | One member made all commits; others absent; GitHub profiles of 2 members show zero contributions; no README stating who did what |
| 2 | 2.5 | 2 of 3 members have some commits; contributions highly uneven; roles mentioned in README but not reflected in history |
| 3 | 3.75 | All 3 members have meaningful commits; rough role split visible (app dev / security / report); README documents individual contributions; distributed activity in contribution graph |
| 4 | 5 | All 3 members with significant, balanced contributions; clear roles documented — each member owns identifiable deliverables; commit messages identify responsibility areas; viva confirms individual knowledge; GitHub Insights verifies distributed work |

---

### 10 — Presentation & Live Demo (25 marks)

| Level | Marks | Criteria |
|---|---|---|
| 1 | 6.25 | Unable to run live demo or app is broken; only one member speaks; reads directly from slides; unable to answer basic Q&A; no security findings demonstrated live |
| 2 | 12.5 | Basic demo runs with some features shown; surface-level explanation of SAST/DAST/SCA results; some questions answered; executive summary remains technical; two members contribute |
| 3 | 18.75 | Smooth live demo — all core features and RBAC shown; security findings clearly explained with evidence; all three members present different sections; most Q&A handled confidently; pipeline shown live or with recorded output; demo on HTTPS (self-signed or real cert) |
| 4 | 25 | Professional, confident presentation by all 3 members; live demo: full app + live attack + fix proof; pipeline shown triggering SAST/DAST/SCA in CI; executive summary delivered in plain language; all Q&A answered; each member demonstrates their specific work; time well managed; demo on HTTPS |

---

## Group vs Individual Split

| Component | Weight | Basis |
|---|---|---|
| **Group Mark** | 70% | Quality of shared GitHub repo, report, pipeline, and findings. All group members receive the same base score for criteria 01–09. |
| **Viva Mark** | 30% | Assessed individually during the live presentation. Each member must demonstrate understanding of their own contribution area. |

> A member who cannot answer questions about their own commits or report section **may receive a reduced individual mark** even if the group work is excellent.

---

## Policies & Guidelines

### AI Usage Policy

Students may use AI tools (Claude, GPT, etc.) to assist with development and tooling. However:

- **All findings must be understood.** During viva, students must explain why a vulnerability exists, how the exploit works, and why the fix resolves the root cause — not just read AI-generated text.
- **All code is your responsibility.** AI-assisted code with unexplained security flaws that were not discovered or reported will be treated as a gap in vulnerability discovery depth.

### GitHub Push Guideline

All pushes must follow the issue-association format specified on the course Canvas page. Commits without linked issues will **not** count toward criterion 08.

**Format:**
```
fix #12: sanitise SQL input in login form
```

- Each push must map to an open issue created **before** the work begins — not retroactively.
- Full guideline: [Canvas — Associating Issues with Git Pushes](https://hulms.instructure.com/courses/4979/pages/associating-issues-with-your-gitpushes?module_item_id=186694)
