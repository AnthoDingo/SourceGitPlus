#!/usr/bin/env bash

set -e
set -o
set -u
set pipefail

# ICU versions to support (Debian has no virtual package, must list all)
# Format: space-separated version numbers
ICU_VERSIONS="78 77 76 74 72 71 70 69 68 67 66 65 63"

arch=
appimage_arch=
target=
case "$RUNTIME" in
    linux-x64)
        arch=amd64
        appimage_arch=x86_64
        target=x86_64;;
    linux-arm64)
        arch=arm64
        appimage_arch=arm_aarch64
        target=aarch64;;
    *)
        echo "Unknown runtime $RUNTIME"
        exit 1;;
esac

APPIMAGETOOL_URL=https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage

cd build

if [[ ! -f "appimagetool" ]]; then
    curl -o appimagetool -L "$APPIMAGETOOL_URL"
    chmod +x appimagetool
fi

rm -f SourceGitPlus/*.dbg
rm -f SourceGitPlus/*.pdb
chmod 755 -R SourceGitPlus

mkdir -p SourceGitPlus.AppDir/opt
mkdir -p SourceGitPlus.AppDir/usr/share/metainfo
mkdir -p SourceGitPlus.AppDir/usr/share/applications

cp -r SourceGitPlus SourceGitPlus.AppDir/opt/sourcegitplus
desktop-file-install resources/_common/applications/sourcegitplus.desktop --dir SourceGitPlus.AppDir/usr/share/applications \
    --set-icon com.sourcegit_scm.SourceGit --set-key=Exec --set-value=AppRun
mv SourceGitPlus.AppDir/usr/share/applications/{sourcegitplus,com.sourcegit_scm.SourceGit}.desktop
cp resources/_common/icons/sourcegitplus.png SourceGitPlus.AppDir/com.sourcegit_scm.SourceGit.png
ln -rsf SourceGitPlus.AppDir/opt/sourcegitplus/sourcegitplus SourceGitPlus.AppDir/AppRun
ln -rsf SourceGitPlus.AppDir/usr/share/applications/com.sourcegit_scm.SourceGit.desktop SourceGitPlus.AppDir
cp resources/appimage/sourcegitplus.appdata.xml SourceGitPlus.AppDir/usr/share/metainfo/com.sourcegit_scm.SourceGit.appdata.xml

ARCH="$appimage_arch" ./appimagetool -v \
    -u "gh-releases-zsync|sourcegit-scm|sourcegitplus|latest|sourcegitplus-*.linux.$arch.AppImage.zsync" \
    SourceGitPlus.AppDir "sourcegitplus-$VERSION.linux.$arch.AppImage"

mkdir -p resources/deb/opt/sourcegitplus/
mkdir -p resources/deb/usr/bin
mkdir -p resources/deb/usr/share/applications
mkdir -p resources/deb/usr/share/icons
cp -f SourceGitPlus/* resources/deb/opt/sourcegitplus
ln -rsf resources/deb/opt/sourcegitplus/sourcegitplus resources/deb/usr/bin
cp -r resources/_common/applications resources/deb/usr/share
cp -r resources/_common/icons resources/deb/usr/share

# Calculate installed size in KB
installed_size=$(du -sk resources/deb | cut -f1)

# Generate ICU dependencies string for Debian
# Debian lacks libicu virtual package, must list all versions with OR operator
icu_deps="libicu"
for v in $ICU_VERSIONS; do
    icu_deps="$icu_deps | libicu$v"
done

# Update the control file (replace placeholder, not whole Depends line)
sed -i -e "s/^Version:.*/Version: $VERSION/" \
    -e "s/^Architecture:.*/Architecture: $arch/" \
    -e "s/^Installed-Size:.*/Installed-Size: $installed_size/" \
    -e "s/@ICU_DEPS@/$icu_deps/" \
    resources/deb/DEBIAN/control

# Build deb package with gzip compression
dpkg-deb -Zgzip --root-owner-group --build resources/deb "sourcegitplus_$VERSION-1_$arch.deb"

rpmbuild -bb --target="$target" resources/rpm/SPECS/build.spec --define "_topdir $(pwd)/resources/rpm" --define "_version $VERSION"
mv "resources/rpm/RPMS/$target/sourcegitplus-$VERSION-1.$target.rpm" ./
