#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

git submodule update --init --recursive --remote external/DreambitEngine
dotnet restore DreambitGame.sln

echo "DreambitEngine updated."
