#define MyAppName "Dukan Hisab Next"
#define MyAppVersion "0.1.0-preview"
#define MyAppExeName "DukanHisabNext.exe"

[Setup]
AppId={{B9E13E52-534E-4FD6-91AC-4B8E6AF1AB20}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=TM Mobile Shop
DefaultDirName={localappdata}\Programs\Dukan Hisab Next
DefaultGroupName=Dukan Hisab Next
OutputDir=..\out\installer
OutputBaseFilename=Setup-Dukan-Hisab-Next-Preview
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\out\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Dukan Hisab Next"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Dukan Hisab Next"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Dukan Hisab Next چلائیں"; Flags: nowait postinstall skipifsilent
