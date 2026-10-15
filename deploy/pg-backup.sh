#!/usr/bin/env bash
set -euo pipefail

readonly AWS_PROFILE_NAME=drivinglessons-backup
readonly KEEP_LOCAL_DAYS=7
readonly BACKUP_DIR="${BACKUP_DIR:-/var/backups/drivinglessons}"

export PATH="/snap/bin:$PATH"
cd "$(dirname "$0")"

bucket=$(grep -E '^BACKUP_BUCKET=' .env | cut -d= -f2- | tr -d "\"'" || true)

if [[ -z "$bucket" ]]; then
    echo "BACKUP_BUCKET is not set in .env" >&2
    exit 1
fi

stamp=$(date -u +%Y-%m-%dT%H%M%SZ)
name="drivinglessons-$stamp.dump"
file="$BACKUP_DIR/$name"
partial="$file.partial"
trap 'rm -f "$partial"' EXIT

mkdir -p "$BACKUP_DIR"
./dl-compose.sh exec -T postgres pg_dump --username app --dbname drivinglessons --format custom > "$partial"

if [[ ! -s "$partial" ]]; then
    echo "pg_dump produced an empty file" >&2
    exit 1
fi

mv "$partial" "$file"
aws s3 cp "$file" "s3://$bucket/daily/$name" --profile "$AWS_PROFILE_NAME" --only-show-errors --no-progress
find "$BACKUP_DIR" -name 'drivinglessons-*.dump' -mtime +"$KEEP_LOCAL_DAYS" -delete

echo "backup uploaded: s3://$bucket/daily/$name ($(stat -c %s "$file") bytes)"
