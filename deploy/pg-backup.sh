#!/usr/bin/env bash
set -euo pipefail

readonly AWS_PROFILE_NAME=drivinglessons-backup
readonly KEEP_LOCAL_DUMPS=7
readonly BACKUP_DIR="${BACKUP_DIR:-/var/backups/drivinglessons}"

export PATH="/snap/bin:$PATH"
cd "$(dirname "$0")"

partial=""

on_exit() {
    local status=$?

    if [[ -n "$partial" ]]; then
        rm -f "$partial"
    fi

    if (( status != 0 )); then
        echo "backup FAILED (exit $status)" >&2
    fi
}

trap on_exit EXIT

bucket=$(grep -E '^BACKUP_BUCKET=' .env | cut -d= -f2- | tr -d "\"'" || true)

if [[ -z "$bucket" ]]; then
    echo "BACKUP_BUCKET is not set in .env" >&2
    exit 1
fi

stamp=$(date -u +%Y-%m-%dT%H%M%SZ)
name="drivinglessons-$stamp.dump"
file="$BACKUP_DIR/$name"
partial="$file.partial"

mkdir -p "$BACKUP_DIR"
./dl-compose.sh exec -T postgres pg_dump --username app --dbname drivinglessons --format custom > "$partial"

if [[ ! -s "$partial" ]]; then
    echo "pg_dump produced an empty file" >&2
    exit 1
fi

mv "$partial" "$file"
aws s3 cp "$file" "s3://$bucket/daily/$name" --profile "$AWS_PROFILE_NAME" --only-show-errors --no-progress

find "$BACKUP_DIR" -maxdepth 1 -name 'drivinglessons-*.dump' -printf '%f\n' \
    | sort -r \
    | tail -n +$((KEEP_LOCAL_DUMPS + 1)) \
    | while read -r old; do rm -f -- "${BACKUP_DIR:?}/$old"; done

echo "backup uploaded: s3://$bucket/daily/$name ($(stat -c %s "$file") bytes)"
