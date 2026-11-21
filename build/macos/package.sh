#!/usr/bin/env bash
# Builds a self-contained, double-clickable Pathetic.app for Apple silicon.
#
#   build/macos/package.sh [version]
#
# Output: artifacts/Pathetic.app and artifacts/Pathetic-macos-arm64.zip
# Requires the .NET SDK matching Directory.Build.props (net11.0).
set -euo pipefail

VERSION="${1:-0.0.0-dev}"
RID="osx-arm64"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
ARTIFACTS="$ROOT/artifacts"
PUBLISH="$ARTIFACTS/publish"
APP="$ARTIFACTS/Pathetic.app"
ZIP="$ARTIFACTS/Pathetic-macos-arm64.zip"

rm -rf "$PUBLISH" "$APP" "$ZIP"
mkdir -p "$ARTIFACTS"

# Self-contained: the .NET runtime ships inside the bundle, nothing to install.
dotnet publish "$ROOT/Pathetic.App/Pathetic.App.csproj" \
    --configuration Release \
    --runtime "$RID" \
    --self-contained true \
    -p:SelfContained=true \
    -p:PublishSingleFile=true \
    --output "$PUBLISH"

# The app resolves shaders, assets, Pathetic.ini and native libraries relative to its
# executable, so everything lives next to it in Contents/MacOS.
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
rsync -a --exclude '*.pdb' --exclude '*.xml' "$PUBLISH/" "$APP/Contents/MacOS/"
sed "s/__VERSION__/$VERSION/g" "$ROOT/build/macos/Info.plist" > "$APP/Contents/Info.plist"

# Ad-hoc signature: required for arm64 binaries to launch. Not notarized, so Gatekeeper
# asks for confirmation the first time (right-click > Open).
xattr -cr "$APP"
codesign --force --deep --sign - "$APP"
codesign --verify --deep --strict "$APP"

# ditto keeps the bundle's symlinks, permissions and signature intact.
ditto -c -k --keepParent "$APP" "$ZIP"

echo "Built $APP"
echo "Built $ZIP"
