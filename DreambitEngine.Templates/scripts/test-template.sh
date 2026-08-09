#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/DreambitEngine.Templates.csproj"
test_root="$root/TemplateTests"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$project" | head -n 1)"
package="$root/bin/$configuration/DreambitEngine.Templates.$version.nupkg"

rm -rf "$test_root"
mkdir -p "$test_root"

dotnet pack "$project" -c "$configuration"
dotnet new install "$package" --force
(
  cd "$test_root"
  dotnet new dreambit-game -n TemplateSmokeTest --game-title "Template Smoke Test"
)

launcher="$test_root/TemplateSmokeTest/src/TemplateSmokeTest.VK/TemplateSmokeTest.VK.csproj"
project_reference_count="$(grep -c '<ProjectReference ' "$launcher" || true)"
if [[ "$project_reference_count" != "1" ]] || ! grep -q '<ProjectReference Include="../TemplateSmokeTest/TemplateSmokeTest.csproj" />' "$launcher"; then
  echo "The launcher does not contain exactly one game-code ProjectReference." >&2
  exit 1
fi

echo "Template smoke test passed: $test_root/TemplateSmokeTest"
