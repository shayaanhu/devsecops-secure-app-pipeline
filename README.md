This project implements a complete “Build & Break” DevSecOps pipeline where a web application is designed, developed, attacked, and secured. It covers threat modeling, automated security testing (SAST, DAST, SCA), manual penetration testing, vulnerability exploitation, and final remediation. The goal is to simulate a real-world secure software development lifecycle (SDLC) with both offensive and defensive security practices.

## How to Run the App (UniRide)

This is a full-stack application. You will need two separate terminal windows to run it.

### 1. Run the Backend API (ASP.NET Core)
Open your first terminal and run:
```bash
cd backend
dotnet run
```
*(The API will start at `https://localhost:7161` or `http://localhost:5007`)*

### 2. Run the Frontend (Vite / React)
Open a **new, separate terminal** and run:
```bash
cd frontend
npm install   # Only needed the very first time
npm run dev
```
*(The React app will start at `https://localhost:58562`)*
