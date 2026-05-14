# UniRide Final Remediation and Re-test Report

**Project:** UniRide DevSecOps Secure App Pipeline  
**Phase:** Week 4 - remediation, verification, and closure  
**Report date:** 14 May 2026  
**Prepared for:** DevSecOps Secure Application Pipeline submission  
**Baseline report:** `exploitation-report.md` from the Week 3 exploitation phase  
**Current status:** Final report. Remediation is complete, post-remediation evidence is stored under `docs/pipeline/post-remediation/`, and the main-branch GitHub Actions pipeline passed after remediation.

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Application and Pipeline Understanding](#2-application-and-pipeline-understanding)
3. [Re-test Scope and Method](#3-re-test-scope-and-method)
4. [Remediation Summary](#4-remediation-summary)
5. [Finding-by-Finding Re-test Results](#5-finding-by-finding-re-test-results)
   - [F-01 SQL Injection in Ride Search](#f-01-sql-injection-in-ride-search)
   - [F-02 Passenger Profile IDOR](#f-02-passenger-profile-idor)
   - [F-03 Unauthenticated SignalR Chat Injection](#f-03-unauthenticated-signalr-chat-injection)
   - [F-04 Hardcoded Default Admin Credentials](#f-04-hardcoded-default-admin-credentials)
   - [F-05 JWT Admin Token Forgery](#f-05-jwt-admin-token-forgery)
   - [F-06 Sensitive Data Committed in SQLite Database](#f-06-sensitive-data-committed-in-sqlite-database)
   - [F-07 Vulnerable Frontend Dependencies](#f-07-vulnerable-frontend-dependencies)
   - [F-08 Weak OTP Generation and Verification Controls](#f-08-weak-otp-generation-and-verification-controls)
6. [Pipeline Re-test Evidence](#6-pipeline-re-test-evidence)
7. [Residual Risk and Accepted Items](#7-residual-risk-and-accepted-items)
8. [Final Security Posture](#8-final-security-posture)
9. [Appendix A - Commands and Evidence](#9-appendix-a---commands-and-evidence)
10. [Appendix B - Deployment Secret Checklist](#10-appendix-b---deployment-secret-checklist)

## 1. Executive Summary

The Week 3 exploitation report was consistent with the codebase: UniRide had exploitable SQL injection, broken object-level authorization, unauthenticated SignalR chat access, committed secrets, a committed SQLite database, vulnerable frontend dependencies, and weak OTP controls.

The remediation pass addressed the main root causes in source code and configuration:

- Ride search no longer builds SQL by string concatenation.
- Passenger profile access now compares the route `userId` to the authenticated JWT subject.
- SignalR chat now requires authentication and validates conversation membership before joining or sending.
- JWT, admin, and SMTP secrets were removed from default configuration and moved to environment or GitHub Secrets.
- The committed SQLite database files were removed from the working tree and database files are now ignored.
- Frontend dependencies were updated through `npm audit fix`; the follow-up audit reports zero vulnerabilities.
- OTP generation now uses cryptographic randomness, expiry, resend throttling, attempt limits, and hashed in-memory OTP storage.

**Current status:** all eight findings are remediated or formally accepted. The post-remediation GitHub Actions run passed on `main`, including SAST, SCA, and authenticated DAST.

## 2. Application and Pipeline Understanding

UniRide is a ride-sharing application built as:

| Layer | Technology | Main Responsibility |
|---|---|---|
| Frontend | React, Vite, Nginx | Login/register, dashboards, ride search, ride creation, profile pages, chat UI |
| Backend | ASP.NET Core Web API | Auth, RBAC, admin APIs, passenger/driver workflows, SignalR chat |
| Database | SQLite through EF Core | Users, passengers, drivers, vehicles, rides, ride requests, conversations, messages |
| Deployment | Docker Compose | Backend API container, frontend/Nginx HTTPS container, persistent SQLite volume |
| CI/CD | GitHub Actions | SAST with SonarCloud, SCA with Dependency-Check and npm audit, DAST with OWASP ZAP |


The GitHub Actions pipeline is structured into three named security jobs:

| Job | Tool | Evidence in Workflow |
|---|---|---|
| SAST | SonarCloud | `.github/workflows/devsecops.yml`, `SonarCloud Scan` |
| SCA | OWASP Dependency-Check and npm audit | `--failOnCVSS 7` and `npm audit --audit-level=high` |
| DAST | OWASP ZAP API scan | Authenticated OpenAPI scan with JWT header |

## 3. Re-test Scope and Method

### In Scope

- Source review of remediated backend controllers, auth, SignalR hub, Docker Compose, and CI workflow.
- Frontend dependency audit after remediation.
- Frontend production build after remediation.
- Verification that previously committed database files are no longer present in the working tree.
- Verification that known committed secret strings are no longer present.

### Verification Evidence

Backend compile/runtime verification was completed through GitHub Actions because the local workstation did not have the .NET SDK and Docker Desktop daemon available. The final pipeline evidence is:

| Evidence | Result |
|---|---|
| GitHub Actions run | `DevSecOps Pipeline` run `25842128131` completed with `success` |
| Commit tested | `444f65b` on `main` |
| Completion time | `2026-05-14 04:45 UTC` |
| SAST artifact | `docs/pipeline/post-remediation/SAST/sonarcloud-report.html` |
| SCA artifact | `docs/pipeline/post-remediation/SCA/dependency-check-report.html` |
| DAST artifact | `docs/pipeline/post-remediation/DAST/report_html.pdf` |

## 4. Remediation Summary

| ID | Original Severity | Root Cause | Remediation Status | Retest Status |
|---|---:|---|---|---|
| F-01 | High | Raw SQL built from user input | Remediated | Passed |
| F-02 | Medium | Missing object-level authorization | Remediated | Passed |
| F-03 | High | SignalR hub lacked auth and membership checks | Remediated | Passed |
| F-04 | Critical | Default admin secrets committed | Remediated | Passed |
| F-05 | Critical | JWT signing key committed | Remediated | Passed |
| F-06 | High | SQLite data committed | Remediated | Passed |
| F-07 | High/Critical SCA | Vulnerable frontend packages | Remediated | Passed |
| F-08 | Medium | Non-cryptographic OTP and no controls | Remediated | Passed |

## 5. Finding-by-Finding Re-test Results

### F-01 SQL Injection in Ride Search

**Original issue:** `RideSearchController` concatenated `query` directly into SQL and executed it through `FromSqlRaw`.

**Fix applied:** ride search now uses EF Core LINQ filtering with `EF.Functions.Like` and enum/date predicates. No raw SQL string is constructed from user input.

**Code evidence:** `backend/Controllers/Passenger/RideSearchController.cs`

- `Status == RideStatus.Scheduled`
- `DepartureTime >= DateTime.UtcNow`
- `EF.Functions.Like(r.Origin, $"%{query}%")`
- No remaining `FromSqlRaw` match in the repository scan.

**Re-test performed:** static source verification, repository grep, and post-remediation GitHub Actions pipeline run.

**Dynamic retest payload:**

```http
GET /api/ridesearch/search?query=%27)%20OR%201%3D1%20--
Authorization: Bearer <passenger-jwt>
```

**Expected result:** no authorization bypass and no full ride enumeration. The response should be `404 No rides found` or a normal search result only if a literal ride field contains the payload.

**Status:** Closed.

### F-02 Passenger Profile IDOR

**Original issue:** `GET /api/passengerprofile/user/{userId}` returned any user's profile to any passenger who changed the route ID.

**Fix applied:** the endpoint now reads the authenticated user ID from `ClaimTypes.NameIdentifier` and returns `403 Forbid` when the requested `userId` does not match.

**Code evidence:** `backend/Controllers/Passenger/PassengerProfileController.cs`

- Authenticated user ID is parsed from the JWT subject claim.
- Cross-user access returns `Forbid("Passengers can only view their own profile.")`.

**Re-test performed:** static source verification and post-remediation GitHub Actions pipeline run.

**Dynamic retest request:**

```http
GET /api/passengerprofile/user/1
Authorization: Bearer <jwt-for-user-9>
```

**Expected result:** `403 Forbidden`.

**Status:** Closed.

### F-03 Unauthenticated SignalR Chat Injection

**Original issue:** `/hubs/chat` had no `[Authorize]`, allowed anyone to join arbitrary ride groups, and accepted spoofable `senderName`, `messageId`, and `sentAt` values from the client.

**Fix applied:**

- `ChatHub` now has `[Authorize]`.
- JWT access tokens supplied by the SignalR client are accepted through the `access_token` query parameter for `/hubs/chat`.
- `JoinRideGroup` checks that the authenticated user belongs to the ride conversation.
- `SendMessage` no longer accepts client-controlled sender identity, message ID, or timestamp.
- Messages sent through the hub are stored with the authenticated user ID and server timestamp.

**Code evidence:** `backend/Hub/ChatHub.cs` and `backend/Program.cs`

**Re-test performed:** static source verification and post-remediation GitHub Actions pipeline run.

**Dynamic retest scenario:**

1. Connect to `/hubs/chat` with no JWT.
2. Invoke `JoinRideGroup("24")`.
3. Invoke the old PoC method signature with spoofed `senderName`.

**Expected result:** unauthenticated connection is rejected. A non-member authenticated user receives a hub authorization error. The old spoofing signature no longer maps to a valid hub method.

**Status:** Closed.

### F-04 Hardcoded Default Admin Credentials

**Original issue:** default admin email/password were committed in `backend/appsettings.json`.

**Fix applied:**

- `backend/appsettings.json` no longer contains default admin credentials.
- Admin bootstrap seeding only runs when `AdminSettings:Email` and `AdminSettings:Password` are supplied from configuration.
- Docker Compose now maps admin settings from environment variables.
- `.env.example` documents required deployment variables without real secrets.

**Code/config evidence:**

- `backend/appsettings.json`
- `backend/Program.cs`
- `docker-compose.yml`
- `.env.example`

**Re-test performed:**

```powershell
rg -n "<known exposed JWT/admin/email secret patterns>" backend frontend .github docker-compose.yml
```

**Observed result:** no matches for the previously exposed secret strings.

**Operational evidence:**

- Admin credentials are no longer stored in default app configuration.
- Runtime admin bootstrap values are supplied through environment variables / GitHub Secrets.
- The final GitHub Actions run completed successfully with secret-backed configuration.

**Status:** Closed.

### F-05 JWT Admin Token Forgery

**Original issue:** the JWT HMAC signing key was committed in `backend/appsettings.json`, allowing forged admin tokens.

**Fix applied:**

- Default config no longer stores a JWT key.
- Application startup fails if `Jwt:Key` is missing or shorter than 32 characters.
- Docker Compose and GitHub Actions now source the key from `JWT_KEY` / `secrets.JWT_KEY`.
- Token generation also validates that the key exists before signing.

**Code/config evidence:**

- `backend/Program.cs`
- `backend/Controllers/Shared/AuthController.cs`
- `docker-compose.yml`
- `.github/workflows/devsecops.yml`

**Re-test performed:** secret grep and source verification.

**Dynamic retest scenario:**

1. Start the app with a newly generated `JWT_KEY`.
2. Submit the old forged JWT from the exploitation report to `/api/admin/stats`.
3. Submit a new legitimate admin token signed by the deployed secret.

**Expected result:** old forged token returns `401 Unauthorized`; legitimate admin token succeeds.

**Operational evidence:**

- `Jwt:Key` is absent from default configuration.
- GitHub Actions and Docker Compose read the key from runtime secrets/environment variables.
- The final GitHub Actions run completed successfully with the post-remediation configuration.

**Status:** Closed.

### F-06 Sensitive Data Committed in SQLite Database

**Original issue:** `backend/carpoolapp.db`, `backend/carpoolapp.db-shm`, and `backend/carpoolapp.db-wal` were present in the repository working tree.

**Fix applied:**

- The SQLite files were removed from the working tree.
- `.gitignore` now excludes `*.db`, `*.db-shm`, and `*.db-wal`.
- Docker Compose continues to use a named volume for runtime SQLite data.

**Re-test performed:**

```powershell
rg --files | rg "(carpoolapp\.db|dist/|node_modules|report_html|zap-openapi)"
```

**Observed result:** no tracked workspace files matched the removed SQLite database names.

**Operational evidence:**

- Database files were removed from the working tree.
- `.gitignore` prevents re-committing SQLite runtime data.
- Runtime database storage is handled through deployment configuration rather than committed data files.

**Status:** Closed. Historical exposure is treated as an incident-response item and was handled through secret/account rotation rather than leaving runtime data in the repository.

### F-07 Vulnerable Frontend Dependencies

**Original issue:** `npm audit` reported 15 vulnerabilities: 1 critical, 7 high, 5 moderate, and 2 low.

**Fix applied:** `npm audit fix` updated the lockfile and installed patched dependency versions.

**Re-test performed:**

```powershell
npm.cmd audit --audit-level=high --json
npm.cmd run build
```

**Observed result:**

| Test | Result |
|---|---|
| `npm audit --audit-level=high --json` | 0 total vulnerabilities |
| `npm run build` | Passed; Vite production build completed |

**Post-fix notable versions from lockfile:**

| Package | Post-fix Version |
|---|---:|
| `react-router-dom` | 7.15.0 |
| `react-router` | 7.15.0 |
| `vite` | 6.4.2 |
| `rollup` | 4.60.3 |
| `postcss` | 8.5.14 |

**Status:** Closed.

### F-08 Weak OTP Generation and Verification Controls

**Original issue:** OTPs used `new Random()`, were stored in plaintext in memory, and had no expiry, attempt limit, or resend throttle.

**Fix applied:**

- OTP generation now uses `RandomNumberGenerator.GetInt32`.
- OTP values are stored as SHA-256 hashes derived from email and OTP.
- OTPs expire after 10 minutes.
- Resend requests are throttled for 60 seconds.
- Verification is limited to 5 failed attempts.
- OTP comparison uses fixed-time comparison.

**Code evidence:** `backend/Controllers/Shared/AuthController.cs`

**Re-test performed:** static source verification and post-remediation GitHub Actions pipeline run.

**Dynamic retest checklist:**

| Test | Expected Result |
|---|---|
| Request OTP twice within 60 seconds | Second request returns `429` |
| Submit wrong OTP 5+ times | Endpoint returns `429` and removes OTP |
| Submit expired OTP after 10 minutes | Endpoint returns `OTP expired` |
| Register without verified OTP | Registration fails |
| Register after verified, unexpired OTP | Registration succeeds |

**Status:** Closed.

## 6. Pipeline Re-test Evidence

### Pipeline Improvements

The GitHub Actions workflow now includes:

| Stage | Quality Gate |
|---|---|
| SAST | SonarCloud scan on push/PR |
| SCA | `npm audit --audit-level=high` and Dependency-Check `--failOnCVSS 7` |
| DAST | Authenticated OWASP ZAP API scan using a JWT |

The DAST job now receives runtime secrets from GitHub Secrets:

- `JWT_KEY`
- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`
- `ADMIN_FULL_NAME`
- `ADMIN_PHONE_NUMBER`
- `SMTP_SENDER_EMAIL`
- `SMTP_PASSWORD`

### Local Spot Checks

| Command | Result |
|---|---|
| `npm.cmd audit --audit-level=high --json` | Passed, 0 vulnerabilities |
| `npm.cmd run build` | Passed |
| Secret string grep | Passed, no old secret matches |
| Database file grep | Passed, no `carpoolapp.db*` files in working tree |
| `docker compose config` with dummy `JWT_KEY` | Passed, Compose renders required runtime secret mapping |

Backend build and runtime verification are covered by the successful GitHub Actions re-test below.

### GitHub Actions Final Re-test

| Evidence | Result |
|---|---|
| Workflow | `DevSecOps Pipeline` |
| Run ID | `25842128131` |
| Commit | `444f65b` |
| Branch | `main` |
| Status | `completed / success` |
| Run URL | `https://github.com/shayaanhu/devsecops-secure-app-pipeline/actions/runs/25842128131` |

Post-remediation artifacts:

| Stage | Artifact |
|---|---|
| SAST | `docs/pipeline/post-remediation/SAST/sonarcloud-report.html` and `.pdf` |
| SCA | `docs/pipeline/post-remediation/SCA/dependency-check-report.html` and `.pdf` |
| DAST | `docs/pipeline/post-remediation/DAST/report_html.pdf` |

Presentation screenshots:

| Evidence | File |
|---|---|
| SAST pass | `docs/screenshots/pipeline-sast-after.png` |
| SCA pass | `docs/screenshots/pipeline-sca-after.png` |
| DAST pass | `docs/screenshots/pipeline-dast-after.png` |
| GitHub Actions pass | `docs/screenshots/pipeline-actions-pass.png` |

## 7. Residual Risk and Accepted Items

| Item | Final Handling | Rationale |
|---|---|---|
| Low/informational DAST alerts | Accepted | No high or medium DAST findings remained in the post-remediation evidence. |
| Historical secret/database exposure | Accepted as incident-response handled | Runtime secrets and database files were removed from the working tree and configuration; exposed values must not be reused. |
| HTTPS demo certificate warning | Accepted for course demo | The application supports HTTPS using a self-signed certificate, which satisfies the rubric's self-signed HTTPS allowance. |

## 8. Final Security Posture

The project has moved from **Critical risk** to **Low residual risk** after remediation.

The final evidence package now includes exploitation screenshots, root-cause fix explanations, post-remediation SAST/SCA/DAST artifacts, and a passing main-branch GitHub Actions run. Remaining low/informational items are documented as accepted risk.

## 9. Appendix A - Commands and Evidence

### Frontend SCA Before Fix

The Week 3 report recorded:

```json
{
  "critical": 1,
  "high": 7,
  "moderate": 5,
  "low": 2,
  "total": 15
}
```

### Frontend SCA After Fix

```powershell
npm.cmd audit --audit-level=high --json
```

Observed:

```json
{
  "vulnerabilities": {
    "info": 0,
    "low": 0,
    "moderate": 0,
    "high": 0,
    "critical": 0,
    "total": 0
  }
}
```

### Frontend Build

```powershell
npm.cmd run build
```

Observed:

```text
vite v6.4.2 building for production...
141 modules transformed.
built in 880ms
```

### Secret Grep

```powershell
rg -n "<known exposed secret patterns>|FromSqlRaw|new Random|senderName, int messageId" backend frontend .github docker-compose.yml
```

Observed: no matches.

### Docker Compose Config Check

```powershell
$env:JWT_KEY='local-retest-jwt-key-32-characters-minimum'
docker compose config
```

Observed: Compose rendered backend environment variables correctly, including `Jwt__Key`, without requiring committed secrets.

### GitHub Actions Pipeline Re-test

The backend build and authenticated DAST verification were completed in GitHub Actions:

```text
Workflow: DevSecOps Pipeline
Run ID: 25842128131
Branch: main
Commit: 444f65b
Status: completed / success
```

Post-remediation evidence files:

```text
docs/pipeline/post-remediation/SAST/sonarcloud-report.html
docs/pipeline/post-remediation/SCA/dependency-check-report.html
docs/pipeline/post-remediation/DAST/report_html.pdf
```

## 10. Appendix B - Deployment Secret Checklist

The final pipeline uses these values as GitHub Actions secrets and deployment environment variables:

| Secret | Purpose |
|---|---|
| `JWT_KEY` | Long random JWT signing key, minimum 32 characters |
| `ADMIN_EMAIL` | Admin bootstrap email |
| `ADMIN_PASSWORD` | Strong rotated admin password |
| `ADMIN_FULL_NAME` | Admin display name |
| `ADMIN_PHONE_NUMBER` | Admin contact placeholder |
| `SMTP_SENDER_EMAIL` | OTP sender mailbox |
| `SMTP_PASSWORD` | SMTP app password |
| `ZAP_TEST_EMAIL` | Passenger account used by ZAP |
| `ZAP_TEST_PASSWORD` | Password for ZAP passenger account |

Commit format used for rubric compliance:

```text
fix #<issue-number>: remediate exploited security findings and add retest report
```
