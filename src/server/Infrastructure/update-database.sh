#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

dotnet tool restore
dotnet ef database update ${1:+"$1"} \
  --project . \
  --startup-project ../Api
