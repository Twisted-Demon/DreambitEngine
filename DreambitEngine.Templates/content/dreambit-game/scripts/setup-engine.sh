#!/usr/bin/env bash
set -euo pipefail

repository="${1:-__DREAMBIT_ENGINE_REPOSITORY__}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
engine_relative="external/DreambitEngine"
engine="$root/$engine_relative"
engine_project="$engine/DreambitEngine/DreambitEngine.csproj"

cd "$root"

command -v git >/dev/null 2>&1 || {
  echo "Git is required. Install Git and run this script again." >&2
  exit 1
}

if [[ ! -d .git ]]; then
  git init
fi

if [[ -f "$engine_project" ]]; then
  echo "DreambitEngine already exists at $engine"
elif [[ -d "$engine" ]] && [[ -n "$(find "$engine" -mindepth 1 -maxdepth 1 -print -quit)" ]]; then
  echo "'$engine' exists but is not a valid DreambitEngine checkout." >&2
  exit 1
else
  rm -rf "$engine"
  mkdir -p "$(dirname "$engine")"
  git submodule add "$repository" "$engine_relative"
  git submodule update --init --recursive
fi

dotnet restore DreambitGame.sln
dotnet build src/DreambitGame.VK/DreambitGame.VK.csproj

echo "Dreambit Game is ready."
echo "Run it with: dotnet run --project src/DreambitGame.VK"
