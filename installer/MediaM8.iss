#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId=Zeekroket.MediaM8
AppName=Media M8
AppVersion={#AppVersion}
AppPublisher=Zeekroket
AppPublisherURL=https://github.com/Zeekroket/Media-M8
AppSupportURL=https://github.com/Zeekroket/Media-M8/issues
AppUpdatesURL=https://github.com/Zeekroket/Media-M8/releases
DefaultDirName={autopf}\Media M8
DefaultGroupName=Media M8
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\MediaM8.exe
SetupIconFile=..\assets\MediaM8.ico
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=MediaM8-{#AppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\dist\MediaM8.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Media M8"; Filename: "{app}\MediaM8.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\MediaM8.exe"; Description: "Launch Media M8"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if FileExists(ExpandConstant('{app}\portable.flag')) then
    Result := 'This folder contains a portable copy of Media M8. Choose a different installation folder to keep the two copies separate.';
end;
