; Usage: ISCC /DAppVersion=2026.23 /DRuntime=win-x64 /DSourceDir=<publish dir> /DOutDir=<dir> SourceGit.iss
#ifndef AppVersion
  #define AppVersion "0.0"
#endif
#ifndef Runtime
  #define Runtime "win-x64"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\publish\" + Runtime
#endif
#ifndef OutDir
  #define OutDir "..\..\dist"
#endif

[Setup]
AppId={{B7C1F7E4-3D2A-4C55-9A53-5F0B6E1D7A11}
AppName=SourceGit Plus
AppVersion={#AppVersion}
AppPublisher=sourcegit-scm
AppPublisherURL=https://github.com/sourcegit-scm/sourcegit
DefaultDirName={autopf}\SourceGit Plus
DefaultGroupName=SourceGit Plus
UninstallDisplayIcon={app}\SourceGit.exe
OutputDir={#OutDir}
OutputBaseFilename=SourceGitPlus-{#AppVersion}-{#Runtime}-setup
LicenseFile=..\..\LICENSE
SetupIconFile=..\..\src\App.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; per-user by default, the user can choose "for all users" in the dialog
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
#if Runtime == "win-arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\SourceGit Plus"; Filename: "{app}\SourceGit.exe"
Name: "{autodesktop}\SourceGit Plus"; Filename: "{app}\SourceGit.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SourceGit.exe"; Description: "{cm:LaunchProgram,SourceGit Plus}"; Flags: nowait postinstall skipifsilent
