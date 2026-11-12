#!/usr/bin/env bash

set -e
set -o
set -u
set pipefail

cd build

mkdir -p SourceGitPlus.app/Contents/Resources
mv SourceGitPlus SourceGitPlus.app/Contents/MacOS
cp resources/app/App.icns SourceGitPlus.app/Contents/Resources/App.icns
sed "s/SOURCE_GIT_VERSION/$VERSION/g" resources/app/App.plist > SourceGitPlus.app/Contents/Info.plist
rm -rf SourceGitPlus.app/Contents/MacOS/SourceGitPlus.dsym
rm -f SourceGitPlus.app/Contents/MacOS/*.pdb

case "$RUNTIME" in
  osx-arm64) ARCH=arm64 ;;
  osx-x64)   ARCH=x86_64 ;;
  *) echo "Unsupported RUNTIME: $RUNTIME" >&2; exit 1 ;;
esac

clang -arch "$ARCH" ../tools/setsid-macos/setsid.c -o SourceGitPlus.app/Contents/MacOS/setsid -mmacosx-version-min=13.0
codesign --force --deep --sign - SourceGitPlus.app
codesign --verify --deep --strict --verbose=2 SourceGitPlus.app

zip "sourcegitplus_$VERSION.$RUNTIME.zip" -r SourceGitPlus.app
