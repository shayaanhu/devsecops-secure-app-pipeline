# UniRide

A university carpooling platform — built, broken, and secured.

UniRide connects Habib University students as drivers and passengers for shared commutes. The platform handles ride creation, seat booking, real-time in-app messaging, and a full admin panel — all authenticated via university email OTP and JWT-based RBAC.

This repository is the deliverable for the **DevSecOps Secure Application Pipeline** project (Cybersecurity, Semester 8, Habib University). The codebase was deliberately built with exploitable vulnerabilities, then subjected to a full security pipeline (SAST, SCA, DAST) and manual penetration testing before being remediated.

---

## Team

| Name | Role |
|---|---|
| Muhammad Shayaan | App Development & Pipeline |
| Muhammad Hayyan Khan | Security & Exploitation |
| Muhammad Wajeeh Haider | Exploitation & Report |
| Ikhlas Ahmed | Threat Model & Documentation |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 8), C# |
| Frontend | React 19, Vite 6 |
| Database | SQLite via Entity Framework Core |
| Auth | JWT HS256, OTP via university email |
| Real-time | SignalR (`/hubs/chat`) |
| Pipeline | GitHub Actions — SonarCloud, OWASP Dependency-Check, OWASP ZAP |

---

## Running Locally

**Backend** (Terminal 1):
```bash
cd backend
dotnet run
```
API: `https://localhost:7161` — Swagger: `https://localhost:7161/swagger`

**Frontend** (Terminal 2):
```bash
cd frontend
npm install
npm run dev
```
App: `https://localhost:58562`

> First run: EF Core migrations are applied automatically on startup.

---

## Security Pipeline

Three automated gates trigger on every push and pull request to `main`:

| Stage | Tool | Threshold |
|---|---|---|
| SAST | SonarCloud | Quality gate on security rating |
| SCA | OWASP Dependency-Check | Fails on CVSS ≥ 7.0 |
| DAST | OWASP ZAP | Authenticated API scan via JWT + OpenAPI |

Pre-remediation reports are archived under `docs/pipeline/pre-remediation/`.

---

## Repository Structure

```
├── backend/          ASP.NET Core Web API
├── frontend/         React 19 SPA
├── docs/
│   ├── architecture/ System architecture diagram
│   ├── threat_modeling/ STRIDE threat model + report
│   └── pipeline/
│       └── pre-remediation/ SAST, SCA, DAST reports before fixes
├── tools/            Local report generation scripts
├── .github/workflows/devsecops.yml  CI/CD pipeline
└── presentation.html Live presentation (Reveal.js)
```

---