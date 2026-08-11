#!/usr/bin/env bash
set -euo pipefail

configuration="${1:-Release}"
template_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
engine_root="$(cd "$template_root/.." && pwd)"
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$template_root/DreambitEngine.Templates.csproj" | head -n 1)"
data_root="${XDG_DATA_HOME:-$HOME/.local/share}"
feed="$data_root/Dreambit/Editor/sdks/$version/packages"

mkdir -p "$feed"
dotnet pack "$engine_root/DreambitEngine/DreambitEngine.csproj" -c "$configuration" -p:PackageVersion="$version" -o "$feed" --nologo
dotnet pack "$engine_root/DreambitEngine.Build/DreambitEngine.Build.csproj" -c "$configuration" -p:PackageVersion="$version" -o "$feed" --nologo
dotnet pack "$template_root/DreambitEngine.Templates.csproj" -c "$configuration" -p:PackageVersion="$version" -o "$feed" --nologo

dotnet new install "$feed/DreambitEngine.Templates.$version.nupkg" --force
echo "Installed Dreambit SDK $version at $feed."
echo 'Create a game with: dotnet new dreambit-game -n MyGame --game-title "My Game"'
