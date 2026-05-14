# UniRide Final Remediation and Re-test Report

**Project:** UniRide DevSecOps Secure App Pipeline  
**Phase:** Week 4 - remediation, verification, and closure  
**Report date:** 13 May 2026  
**Prepared for:** DevSecOps Secure Application Pipeline submission  
**Baseline report:** `exploitation-report.md` from the Week 3 exploitation phase  

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
7. [Residual Risk and Required Submission Evidence](#7-residual-risk-and-required-submission-evidence)
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

**Current status:** code-level remediation is complete for all eight findings. Frontend SCA was dynamically retested and passed. Backend dynamic API retesting must still be captured in GitHub Actions or on the deployed HTTPS host because this workstation has no .NET SDK available and Docker Desktop is not running.

## 2. Application and Pipeline Understanding

UniRide is a ride-sharing application built as:

| Layer | Technology | Main Responsibility |
|---|---|---|
| Frontend | React, Vite, Nginx | Login/register, dashboards, ride search, ride creation, profile pages, chat UI |
| Backend | ASP.NET Core Web API | Auth, RBAC, admin APIs, passenger/driver workflows, SignalR chat |
| Database | SQLite through EF Core | Users, passengers, drivers, vehicles, rides, ride requests, conversations, messages |
| Deployment | Docker Compose | Backend API container, frontend/Nginx HTTPS container, persistent SQLite volume |
| CI/CD | GitHub Actions | SAST with SonarCloud, SCA with Dependency-Check and npm audit, DAST with OWASP ZAP |

The application baseline required by the rubric is present in the project design: RBAC roles, login/logout with JWT revocation support, admin and non-admin pages, CRUD-style ride/user operations, ride search/filtering, and a connected database.

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

### Verification Limits

Backend compile/runtime re-test could not be completed on this workstation:

| Check | Result |
|---|---|
| `dotnet build backend\UniRide.Api.csproj --configuration Release` | Failed because no .NET SDK is installed; only runtime is available. |
| `docker compose build backend` | Failed because Docker Desktop daemon is not running. |

Because of this, backend findings are marked **Source remediated, dynamic retest pending** until the GitHub Actions DAST job or a deployed HTTPS environment captures live API evidence.

## 4. Remediation Summary

| ID | Original Severity | Root Cause | Remediation Status | Retest Status |
|---|---:|---|---|---|
| F-01 | High | Raw SQL built from user input | Source remediated | Dynamic retest pending |
| F-02 | Medium | Missing object-level authorization | Source remediated | Dynamic retest pending |
| F-03 | High | SignalR hub lacked auth and membership checks | Source remediated | Dynamic retest pending |
| F-04 | Critical | Default admin secrets committed | Source remediated; rotation/history purge still required | Operational evidence pending |
| F-05 | Critical | JWT signing key committed | Source remediated; key rotation required | Operational evidence pending |
| F-06 | High | SQLite data committed | Working tree remediated; Git history purge still required | Partially verified |
| F-07 | High/Critical SCA | Vulnerable frontend packages | Remediated | Passed |
| F-08 | Medium | Non-cryptographic OTP and no controls | Source remediated | Dynamic retest pending |

## 5. Finding-by-Finding Re-test Results

### F-01 SQL Injection in Ride Search

**Original issue:** `RideSearchController` concatenated `query` directly into SQL and executed it through `FromSqlRaw`.

**Fix applied:** ride search now uses EF Core LINQ filtering with `EF.Functions.Like` and enum/date predicates. No raw SQL string is constructed from user input.

**Code evidence:** `backend/Controllers/Passenger/RideSearchController.cs`

- `Status == RideStatus.Scheduled`
- `DepartureTime >= DateTime.UtcNow`
- `EF.Functions.Like(r.Origin, $"%{query}%")`
- No remaining `FromSqlRaw` match in the repository scan.

**Re-test performed:** static source verification and repository grep.

**Expected dynamic retest:**

```http
GET /api/ridesearch/search?query=%27)%20OR%201%3D1%20--
Authorization: Bearer <passenger-jwt>
```

**Expected result:** no authorization bypass and no full ride enumeration. The response should be `404 No rides found` or a normal search result only if a literal ride field contains the payload.

**Status:** Source remediated; dynamic API evidence still required.

### F-02 Passenger Profile IDOR

**Original issue:** `GET /api/passengerprofile/user/{userId}` returned any user's profile to any passenger who changed the route ID.

**Fix applied:** the endpoint now reads the authenticated user ID from `ClaimTypes.NameIdentifier` and returns `403 Forbid` when the requested `userId` does not match.

**Code evidence:** `backend/Controllers/Passenger/PassengerProfileController.cs`

- Authenticated user ID is parsed from the JWT subject claim.
- Cross-user access returns `Forbid("Passengers can only view their own profile.")`.

**Re-test performed:** static source verification.

**Expected dynamic retest:**

```http
GET /api/passengerprofile/user/1
Authorization: Bearer <jwt-for-user-9>
```

**Expected result:** `403 Forbidden`.

**Status:** Source remediated; dynamic API evidence still required.

### F-03 Unauthenticated SignalR Chat Injection

**Original issue:** `/hubs/chat` had no `[Authorize]`, allowed anyone to join arbitrary ride groups, and accepted spoofable `senderName`, `messageId`, and `sentAt` values from the client.

**Fix applied:**

- `ChatHub` now has `[Authorize]`.
- JWT access tokens supplied by the SignalR client are accepted through the `access_token` query parameter for `/hubs/chat`.
- `JoinRideGroup` checks that the authenticated user belongs to the ride conversation.
- `SendMessage` no longer accepts client-controlled sender identity, message ID, or timestamp.
- Messages sent through the hub are stored with the authenticated user ID and server timestamp.

**Code evidence:** `backend/Hub/ChatHub.cs` and `backend/Program.cs`

**Re-test performed:** static source verification.

**Expected dynamic retest:**

1. Connect to `/hubs/chat` with no JWT.
2. Invoke `JoinRideGroup("24")`.
3. Invoke the old PoC method signature with spoofed `senderName`.

**Expected result:** unauthenticated connection is rejected. A non-member authenticated user receives a hub authorization error. The old spoofing signature no longer maps to a valid hub method.

**Status:** Source remediated; dynamic SignalR evidence still required.

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

**Required operational evidence before final submission:**

- Rotate the old admin password everywhere it may have been used.
- Add `ADMIN_EMAIL` and `ADMIN_PASSWORD` to GitHub Secrets or the deployment host.
- Capture failed login evidence for the old default credentials.

**Status:** Source remediated; credential rotation evidence pending.

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

**Expected dynamic retest:**

1. Start the app with a newly generated `JWT_KEY`.
2. Submit the old forged JWT from the exploitation report to `/api/admin/stats`.
3. Submit a new legitimate admin token signed by the deployed secret.

**Expected result:** old forged token returns `401 Unauthorized`; legitimate admin token succeeds.

**Required operational evidence before final submission:**

- Rotate `JWT_KEY` in GitHub/deployment secrets.
- Invalidate tokens issued with the old key.
- Purge the old key from Git history if the repository was public.

**Status:** Source remediated; key rotation and dynamic token rejection evidence pending.

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

**Required operational evidence before final submission:**

- Commit the deletions.
- If the repository was ever public, purge database files from Git history using a history rewriting tool such as `git filter-repo` or BFG.
- Rotate or reset any accounts whose PII/password hashes were in the database.

**Status:** Working tree remediated; Git history purge evidence pending.

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

**Re-test performed:** static source verification.

**Expected dynamic retest:**

| Test | Expected Result |
|---|---|
| Request OTP twice within 60 seconds | Second request returns `429` |
| Submit wrong OTP 5+ times | Endpoint returns `429` and removes OTP |
| Submit expired OTP after 10 minutes | Endpoint returns `OTP expired` |
| Register without verified OTP | Registration fails |
| Register after verified, unexpired OTP | Registration succeeds |

**Status:** Source remediated; dynamic API evidence still required.

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

### Local Verification Results

| Command | Result |
|---|---|
| `npm.cmd audit --audit-level=high --json` | Passed, 0 vulnerabilities |
| `npm.cmd run build` | Passed |
| Secret string grep | Passed, no old secret matches |
| Database file grep | Passed, no `carpoolapp.db*` files in working tree |
| `docker compose config` with dummy `JWT_KEY` | Passed, Compose renders required runtime secret mapping |
| `dotnet build` | Not run successfully; .NET SDK unavailable |
| `docker compose build backend` | Not run successfully; Docker daemon unavailable |

## 7. Residual Risk and Required Submission Evidence

| Item | Risk if Not Completed | Required Evidence |
|---|---|---|
| Backend dynamic retest | Code fixes may contain unobserved runtime defects | GitHub Actions DAST pass or deployed HTTPS API retest screenshots |
| Secret rotation | Old leaked secrets may still work somewhere | Screenshot of updated GitHub Secrets/deployment environment and failed old-token/default-login retest |
| Git history purge | Old database and secrets may remain downloadable from repository history | History rewrite commit or GitHub secret scanning result |
| CI pipeline proof | Rubric requires pipeline rerun after fixes | GitHub Actions run showing SAST, SCA, and DAST jobs passing |
| HTTPS demo proof | Rubric requires non-localhost HTTPS demo | Screenshot or recording of deployed HTTPS host/IP |

## 8. Final Security Posture

The project has moved from **Critical risk** to **Medium residual risk** at the source-code level.

The remaining risk is mostly evidence and operations-driven: rotate secrets, purge history, run the backend in CI/deployment, and capture live retest proof. Once those are completed, the remediation posture should meet the rubric's Level 4 expectation for root-cause fixes and retest evidence.

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

### Backend Verification Blockers

```powershell
dotnet build backend\UniRide.Api.csproj --configuration Release
```

Observed:

```text
No .NET SDKs were found.
```

```powershell
docker compose build backend
```

Observed:

```text
failed to connect to the docker API ... Docker Desktop daemon is not running
```

## 10. Appendix B - Deployment Secret Checklist

Before the final demo, configure these values as GitHub Actions secrets and deployment environment variables:

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

Recommended commit format for this remediation:

```text
fix #<issue-number>: remediate exploited security findings and add retest report
```
