#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

exec docker compose \
    --env-file .env \
    --env-file release.env \
    -f docker-compose.yml \
    -f docker-compose.prod.yml \
    "$@"
