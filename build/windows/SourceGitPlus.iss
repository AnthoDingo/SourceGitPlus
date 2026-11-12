; Usage: ISCC /DAppVersion=2026.23 /DRuntime=win-x64 /DSourceDir=<publish dir> /DOutDir=<dir> SourceGitPlus.iss
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
AppName=SourceGitPlus
AppVersion={#AppVersion}
AppPublisher=AnthoDingo
AppPublisherURL=https://github.com/AnthoDingo/SourceGitPlus
AppSupportURL=https://github.com/AnthoDingo/SourceGitPlus/issues
AppUpdatesURL=https://github.com/AnthoDingo/SourceGitPlus/releases
DefaultDirName={autopf}\SourceGitPlus
DefaultGroupName=SourceGitPlus
UninstallDisplayIcon={app}\SourceGitPlus.exe
UninstallDisplayName=SourceGitPlus {#AppVersion}
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
; Updates: the same AppId installs over the previous version, in place. A running
; instance is closed first (the user is asked), and is not restarted automatically.
CloseApplications=yes
CloseApplicationsFilter=SourceGitPlus.exe
RestartApplications=no
SetupLogging=yes
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

[CustomMessages]
english.AddToPath=Add the sgp command to PATH (open a repository from a terminal with: sgp .)
french.AddToPath=Ajouter la commande sgp au PATH (ouvrir un dépôt depuis un terminal avec : sgp .)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "addtopath"; Description: "{cm:AddToPath}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\SourceGitPlus"; Filename: "{app}\SourceGitPlus.exe"
Name: "{autodesktop}\SourceGitPlus"; Filename: "{app}\SourceGitPlus.exe"; Tasks: desktopicon

[Code]
// Adds or removes {app} in the user PATH (HKCU, no admin needed).
function RemovePathEntry(const Dir, Path: string): string;
var
  S: string;
begin
  S := ';' + Path + ';';
  StringChangeEx(S, ';' + Dir + ';', ';', False);
  while Pos(';;', S) > 0 do
    StringChangeEx(S, ';;', ';', True);
  Result := S;
  if (Length(Result) > 0) and (Result[1] = ';') then
    Delete(Result, 1, 1);
  if (Length(Result) > 0) and (Result[Length(Result)] = ';') then
    Delete(Result, Length(Result), 1);
end;

procedure AddToUserPath(const Dir: string);
var
  Path: string;
begin
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', Path) then
    Path := '';
  if Pos(';' + Uppercase(Dir) + ';', ';' + Uppercase(Path) + ';') = 0 then
  begin
    if (Path <> '') and (Path[Length(Path)] <> ';') then
      Path := Path + ';';
    RegWriteExpandStringValue(HKCU, 'Environment', 'Path', Path + Dir);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('addtopath') then
    AddToUserPath(ExpandConstant('{app}'));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Path: string;
begin
  if (CurUninstallStep = usPostUninstall) and RegQueryStringValue(HKCU, 'Environment', 'Path', Path) then
    RegWriteExpandStringValue(HKCU, 'Environment', 'Path', RemovePathEntry(ExpandConstant('{app}'), Path));
end;

[Run]
Filename: "{app}\SourceGitPlus.exe"; Description: "{cm:LaunchProgram,SourceGitPlus}"; Flags: nowait postinstall skipifsilent
