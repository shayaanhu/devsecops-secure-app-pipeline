# Running UniRide (Docker)

## Start

```
docker compose up --build
```

First run builds the images. Subsequent runs skip the build:

```
docker compose up
```

## Access

| URL | Use |
|---|---|
| `https://localhost` | Local dev / same machine |
| `https://192.168.18.223` | Demo / grader / other devices on the same network |

> The app uses a self-signed TLS certificate. Click **Advanced → Proceed** to bypass the browser warning.

## Stop

```
docker compose down
```

## Test Credentials

| Email | Password | Role |
|---|---|---|
| `ms08066@st.habib.edu.pk` | `ZapTest@2026!` | Driver or Passenger |
| `admin@uniride.local` | `Admin@1234` | Admin |

## Logs

```
docker compose logs -f
docker compose logs -f backend
docker compose logs -f frontend
```

## Rebuild a single service

```
docker compose up --build backend
docker compose up --build frontend
```
