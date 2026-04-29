# UniRide — "Build & Break" Secure Application Pipeline

A university carpooling web application built for a DevSecOps course assignment at Habib University. The project implements a full "Build & Break" pipeline: the app is intentionally built with security vulnerabilities, then attacked using SAST, DAST, and SCA tools, then hardened and re-tested.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 8), C# |
| Database | SQLite via Entity Framework Core |
| Authentication | JWT Bearer (HS256, 6-hour expiry) |
| Real-time | SignalR (WebSocket chat) |
| Frontend | React 19, Vite 6 |
| HTTP Client | Axios |
| Routing | React Router v7 |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- A Gmail account with 2FA enabled and an App Password generated (for OTP email delivery)

---

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/shayaanhu/devsecops-secure-app-pipeline.git
cd devsecops-secure-app-pipeline
```

### 2. Trust the development certificate (first time only)

```bash
dotnet dev-certs https --trust
```

This eliminates the "Not Secure" browser warning on `https://localhost:58562`.

---

## Running the App

Two terminals are required.

**Terminal 1 — Backend:**
```bash
cd backend
dotnet run
```
- API: `https://localhost:7161`
- Swagger UI: `https://localhost:7161/swagger`

**Terminal 2 — Frontend:**
```bash
cd frontend
npm install   # first time only
npm run dev
```
- App: `https://localhost:58562`

---

## User Roles

The app has three roles with separate pages and permissions:

| Role | Access | Default Credentials |
|---|---|---|
| **Driver** | Create rides, manage vehicles, accept/reject passenger requests, real-time chat | Register via app |
| **Passenger** | Search and book rides, view ride history, real-time chat | Register via app |
| **Admin** | View all users and rides, delete users, force-cancel rides | `admin@uniride.local` / `Admin@1234` |

The admin account is seeded automatically at backend startup — no registration needed.

---

## Registration Flow

1. Enter your details and university email on the Register page
2. An OTP is sent to that email via the configured Gmail SMTP
3. Enter the OTP to verify
4. Click Complete Registration
5. Login with your email, password, and select a role (Driver or Passenger)

---

## Project Structure

```
devsecops-secure-app-pipeline/
├── backend/                        # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── Admin/                  # AdminController (admin-only endpoints)
│   │   ├── Driver/                 # Dashboard, Profile, RideManagement, RideRequest
│   │   ├── Passenger/              # Booking, Dashboard, Profile, RideSearch
│   │   └── Shared/                 # Auth, Message
│   ├── Models/                     # EF Core entity models
│   ├── DTO/                        # Data transfer objects
│   ├── Data/                       # CarpoolDbContext (SQLite)
│   ├── Hub/                        # ChatHub (SignalR)
│   ├── EmailService/               # SMTP OTP delivery
│   ├── Migrations/                 # EF Core migrations
│   ├── Program.cs                  # App entry point, DI, middleware
│   ├── appsettings.json            # Configuration (JWT, DB, email, admin)
│   └── carpoolapp.db               # SQLite database
├── frontend/                       # React 19 + Vite SPA
│   └── src/
│       ├── components/             # LoginPage, RegisterPage, dashboards, chat
│       └── styles/                 # Per-component CSS
├── docs/
│   ├── architecture/               # Architecture diagram
│   └── threat_modeling/            # Threat model (.tm7) and report (PDF)
├── .github/workflows/              # CI/CD pipeline (SAST + SCA + DAST)
└── README.md
```

---

## CI/CD Security Pipeline

The pipeline runs automatically on every push to `main`.

| Tool | Type | Purpose |
|---|---|---|
| SonarCloud | SAST | Static code analysis — bugs, vulnerabilities, code smells |
| OWASP Dependency-Check | SCA | Scans NuGet and npm packages for known CVEs |
| OWASP ZAP | DAST | Active security scan against the running API with JWT authentication |

Security reports are uploaded as artifacts on each pipeline run.

---

## API Endpoints

### Auth (`/api/auth`)
| Method | Endpoint | Description |
|---|---|---|
| POST | `/send-otp` | Send OTP to university email |
| POST | `/verify-otp` | Verify OTP |
| POST | `/register` | Register new user |
| POST | `/login` | Login and receive JWT |

### Driver (`/api/driver`, `/api/ridemanagement`, `/api/riderequest`)
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/driver/dashboard/rides-with-requests` | All rides with pending/accepted requests |
| GET | `/api/driver/profile/vehicles` | List driver's vehicles |
| POST | `/api/driver/profile/vehicle` | Add a vehicle |
| POST | `/api/ridemanagement/create` | Create a new ride |
| GET | `/api/ridemanagement/accepted-passengers/{rideId}` | Accepted passengers for a ride |
| POST | `/api/riderequest/accept/{requestId}` | Accept a ride request |
| POST | `/api/riderequest/reject/{requestId}` | Reject a ride request |

### Passenger (`/api/passengerdashboard`, `/api/booking`, `/api/ridesearch`)
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/passengerdashboard/available-rides` | Browse available rides |
| POST | `/api/passengerdashboard/request-ride` | Request to join a ride |
| GET | `/api/passengerdashboard/accepted-rides` | View accepted bookings |
| GET | `/api/ridesearch/search` | Search rides by origin/destination |
| GET | `/api/booking/ride-locations/{rideId}` | Get pickup/dropoff options for a ride |

### Admin (`/api/admin`) — requires admin JWT
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/admin/stats` | Total users, rides, active rides |
| GET | `/api/admin/users` | List all users |
| DELETE | `/api/admin/users/{id}` | Delete a user |
| GET | `/api/admin/rides` | List all rides |
| PATCH | `/api/admin/rides/{id}/cancel` | Force-cancel a ride |

### Messaging (`/api/message`, `/hubs/chat`)
| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/message/ride/{rideId}` | Fetch message history for a ride |
| POST | `/api/message/ride/{rideId}/send` | Send a message |
| WS | `/hubs/chat` | SignalR hub for real-time chat |

---

## Assignment Deliverables

- [x] Application with RBAC (admin, driver, passenger)
- [x] Threat model (`docs/threat_modeling/`)
- [ ] CI/CD pipeline (SAST + DAST + SCA)
- [ ] Exploitation report
- [ ] Final remediation + re-test report
- [ ] Executive summary
- [ ] Live demo presentation
