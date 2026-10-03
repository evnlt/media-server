#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 <MigrationName>" >&2
  exit 1
fi

cd "$(dirname "$0")"

dotnet tool restore
dotnet ef migrations add "$1" \
  --project . \
  --startup-project ../Api \
  --output-dir Persistence/Migrations
