Name: sourcegitplus
Version: %_version
Release: 1
Summary: Open-source & Free Git Gui Client
License: MIT
URL: https://sourcegit-scm.github.io/
Source: https://github.com/sourcegit-scm/sourcegit/archive/refs/tags/v%_version.tar.gz
Requires: libX11.so.6()(%{__isa_bits}bit)
Requires: libSM.so.6()(%{__isa_bits}bit)
Requires: libicu
Requires: xdg-utils

%define _build_id_links none

%description
Open-source & Free Git Gui Client

%install
mkdir -p %{buildroot}/opt/sourcegitplus
mkdir -p %{buildroot}/%{_bindir}
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons
cp -f %{_topdir}/../../SourceGitPlus/* %{buildroot}/opt/sourcegitplus/
ln -rsf %{buildroot}/opt/sourcegitplus/sourcegitplus %{buildroot}/%{_bindir}
cp -r %{_topdir}/../_common/applications %{buildroot}/%{_datadir}
cp -r %{_topdir}/../_common/icons %{buildroot}/%{_datadir}
chmod 755 %{buildroot}/%{_datadir}/applications/sourcegitplus.desktop

%files
%dir /opt/sourcegitplus/
/opt/sourcegitplus/*
/usr/share/applications/sourcegitplus.desktop
/usr/share/icons/*
%{_bindir}/sourcegitplus

%changelog
# skip
