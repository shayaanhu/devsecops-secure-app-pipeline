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

| URL | What |
|---|---|
| `https://localhost` | App (accept the self-signed cert warning) |
| `https://192.168.18.223` | Same, from another device on the network |

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
