#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
template_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
engine_root="$(cd "$template_root/.." && pwd)"
test_root="$template_root/TemplateTests"
feed="$test_root/packages"
template_hive="$test_root/.template-hive"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$template_root/DreambitEngine.Templates.csproj" | head -n 1)"
test_name="Dreambit.TemplateSmokeTest"
generated="$test_root/$test_name"

rm -rf "$test_root"
mkdir -p "$feed"

dotnet pack "$engine_root/DreambitEngine/DreambitEngine.csproj" -c "$configuration" -p:PackageVersion="$version" -o "$feed" --nologo
dotnet pack "$engine_root/Dreambit.Editor.Abstractions/Dreambit.Editor.Abstractions.csproj" -c "$configuration" -p:PackageVersion="$version" -o "$feed" --nologo
dotnet pack "$engine_root/DreambitEngine.Build/DreambitEngine.Build.csproj" -c "$configuration" -p:PackageVersion="$version" -o "$feed" --nologo
dotnet pack "$template_root/DreambitEngine.Templates.csproj" -c "$configuration" -p:PackageVersion="$version" -o "$feed" --nologo

dotnet new --debug:custom-hive "$template_hive" install "$feed/DreambitEngine.Templates.$version.nupkg" --force
dotnet new --debug:custom-hive "$template_hive" dreambit-game \
  -n "$test_name" \
  -o "$generated" \
  --game-title "Template Smoke Test" \
  --sdkVersion "$version" \
  --targetRenderer DesktopVK \
  --target-fps 144 \
  --no-update-check

expected_files=(
  ".dreambit/project.json"
  ".editorconfig"
  ".gitignore"
  "Directory.Packages.props"
  "$test_name.sln"
  "src/Directory.Build.props"
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

[[ ! -e "$generated/external" && ! -e "$generated/scripts" && ! -e "$generated/build" ]]
grep -Fq '"targetRenderer": "DesktopVK"' "$generated/.dreambit/project.json"
grep -Fq 'title: "Template Smoke Test"' "$generated/src/$test_name.VK/Program.cs"
grep -Fq 'Core.SetTargetFps(144);' "$generated/src/$test_name.VK/Program.cs"

if grep -R -E \
    --include='*.cs' --include='*.csproj' --include='*.json' --include='*.md' \
    --include='*.props' --include='*.sln' --include='*.targets' \
    '__DREAMBIT_[A-Z_]+__' "$generated"; then
  echo "Generated output contains an unresolved template placeholder." >&2
  exit 1
fi

solution_projects="$(dotnet sln "$generated/$test_name.sln" list | tr '\\' '/')"
for expected_project in \
  "src/$test_name/$test_name.csproj" \
  "src/$test_name.Content/$test_name.Content.csproj" \
  "src/$test_name.VK/$test_name.VK.csproj"; do
  grep -Fxq "$expected_project" <<<"$solution_projects"
done

dotnet restore "$generated/$test_name.sln" -p:RestoreAdditionalProjectSources="$feed" --nologo
imported_sdk_version="$(dotnet msbuild "$generated/src/$test_name.VK/$test_name.VK.csproj" -getProperty:DreambitSdkVersion --nologo | tail -n 1)"
[[ "$imported_sdk_version" == "$version" ]]
dotnet build "$generated/$test_name.sln" --no-restore --nologo

echo "Template smoke test passed: $generated"
