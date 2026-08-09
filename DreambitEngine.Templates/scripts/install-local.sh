#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/DreambitEngine.Templates.csproj"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$project" | head -n 1)"
package="$root/bin/$configuration/DreambitEngine.Templates.$version.nupkg"

dotnet pack "$project" -c "$configuration"
dotnet new install "$package" --force

echo "Installed DreambitEngine.Templates $version."
echo 'Create a game with: dotnet new dreambit-game -n MyGame --game-title "My Game"'
