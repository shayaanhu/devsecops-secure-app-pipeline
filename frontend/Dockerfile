# ── Stage 1: Build ─────────────────────────────────────────────
FROM node:20-alpine AS build

WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .

# Empty string means use relative URLs — Nginx proxies /api/* and /hubs/*
ARG VITE_API_URL=""
ENV VITE_API_URL=${VITE_API_URL}

RUN npm run build

# ── Stage 2: Serve ─────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Generate self-signed TLS certificate at image build time
RUN apk add --no-cache openssl && \
    mkdir -p /etc/nginx/certs && \
    openssl req -x509 -nodes -days 365 \
        -newkey rsa:2048 \
        -keyout /etc/nginx/certs/selfsigned.key \
        -out    /etc/nginx/certs/selfsigned.crt \
        -subj "/C=PK/ST=Sindh/L=Karachi/O=UniRide/CN=localhost"

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80 443

CMD ["nginx", "-g", "daemon off;"]
