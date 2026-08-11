#!/usr/bin/env sh
set -eu

project_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
engine_path="$project_root/.dreambit/engine"
repository="__DREAMBIT_ENGINE_REPOSITORY__"
engine_ref="__DREAMBIT_ENGINE_REF__"

cd "$project_root"
if [ ! -d .git ]; then
  git init
fi

if [ ! -f "$engine_path/DreambitEngine/DreambitEngine.csproj" ]; then
  git -c protocol.file.allow=always clone --no-checkout "$repository" "$engine_path"
fi

if [ -n "$engine_ref" ]; then
  git -C "$engine_path" fetch origin "$engine_ref"
  git -C "$engine_path" checkout --detach FETCH_HEAD
fi

git config -f .gitmodules submodule.dreambit-engine.path .dreambit/engine
git config -f .gitmodules submodule.dreambit-engine.url "$repository"
git add -- .gitmodules
engine_commit=$(git -C "$engine_path" rev-parse HEAD)
git update-index --add --cacheinfo "160000,$engine_commit,.dreambit/engine"
