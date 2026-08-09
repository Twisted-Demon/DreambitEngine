#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/DreambitEngine.Templates.csproj"
test_root="$root/TemplateTests"
template_hive="$test_root/.template-hive"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$project" | head -n 1)"
package="$root/bin/$configuration/DreambitEngine.Templates.$version.nupkg"
test_name="Dreambit.TemplateSmokeTest"
test_repository="https://example.invalid/DreambitEngine.git"
test_fps="144"

rm -rf "$test_root"
mkdir -p "$test_root"

dotnet pack "$project" -c "$configuration"
[[ -f "$package" ]] || {
  echo "Expected template package was not created: $package" >&2
  exit 1
}

dotnet new --debug:custom-hive "$template_hive" install "$package" --force
(
  cd "$test_root"
  dotnet new --debug:custom-hive "$template_hive" dreambit-game \
    -n "$test_name" \
    --game-title "Template Smoke Test" \
    --engine-repository "$test_repository" \
    --target-fps "$test_fps" \
    --no-update-check
)

generated="$test_root/$test_name"
expected_files=(
  ".editorconfig"
  ".gitignore"
  "$test_name.sln"
  "build/$test_name.Content.targets"
  "scripts/setup-engine.ps1"
  "scripts/setup-engine.sh"
  "scripts/update-engine.ps1"
  "scripts/update-engine.sh"
  "src/$test_name/$test_name.csproj"
  "src/$test_name.Content/$test_name.Content.csproj"
  "src/$test_name.VK/$test_name.VK.csproj"
)

for relative_path in "${expected_files[@]}"; do
  [[ -f "$generated/$relative_path" ]] || {
    echo "Generated template is missing '$relative_path'." >&2
    exit 1
  }
done

[[ ! -e "$generated/.template.config" ]] || {
  echo "Generated output contains the template authoring configuration." >&2
  exit 1
}

bash -n "$generated/scripts/setup-engine.sh" "$generated/scripts/update-engine.sh"
grep -Fq 'title: "Template Smoke Test"' "$generated/src/$test_name.VK/Program.cs"
grep -Fq "Core.SetTargetFps($test_fps);" "$generated/src/$test_name.VK/Program.cs"
grep -Fq "$test_repository" "$generated/scripts/setup-engine.sh"

if grep -R -E \
    --include='*.cs' --include='*.csproj' --include='*.json' --include='*.md' \
    --include='*.props' --include='*.ps1' --include='*.sh' --include='*.sln' \
    --include='*.targets' \
    '__DREAMBIT_[A-Z_]+__' "$generated"; then
  echo "Generated output contains an unresolved template placeholder." >&2
  exit 1
fi

launcher="$generated/src/$test_name.VK/$test_name.VK.csproj"
project_reference_count="$(grep -c '<ProjectReference ' "$launcher" || true)"
if [[ "$project_reference_count" != "1" ]] || ! grep -Fq "<ProjectReference Include=\"../$test_name/$test_name.csproj\" />" "$launcher"; then
  echo "The launcher does not contain exactly one game-code ProjectReference." >&2
  exit 1
fi

dotnet msbuild "$launcher" -getProperty:TargetFramework -p:DreambitContentBuildEnabled=false >/dev/null
solution_projects="$(dotnet sln "$generated/$test_name.sln" list | tr '\\' '/')"
expected_solution_projects=(
  "src/$test_name/$test_name.csproj"
  "src/$test_name.Content/$test_name.Content.csproj"
  "src/$test_name.VK/$test_name.VK.csproj"
  "external/DreambitEngine/DreambitEngine/DreambitEngine.csproj"
  "external/DreambitEngine/Dreambit.Content/Dreambit.Content.csproj"
  "external/DreambitEngine/DreambitEngine.AssetBaker/DreambitEngine.AssetBaker.csproj"
)

for expected_project in "${expected_solution_projects[@]}"; do
  grep -Fxq "$expected_project" <<<"$solution_projects" || {
    echo "Generated solution is missing '$expected_project'." >&2
    exit 1
  }
done

echo "Template smoke test passed: $generated"
