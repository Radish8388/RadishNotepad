[Setup]
AppName=Radish Notepad
AppVersion=1.0.1
DefaultDirName={autopf}\Radish\Radish Notepad
DefaultGroupName=Radish
SetupIconFile=icons\edittext3.ico
UninstallDisplayIcon={app}\RadishNotepad.exe
LicenseFile=LICENSE.txt
OutputBaseFilename=RadishNotepadSetup
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
AppPublisher=Radish
AppPublisherURL=https://radish-vert.vercel.app
AppId={{63e106d2-e1f5-4633-af5d-a5a56b3526e8}

[Files]
Source: "bin\Release\net10.0-windows\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Radish Notepad"; Filename: "{app}\RadishNotepad.exe"
Name: "{commondesktop}\Radish Notepad"; Filename: "{app}\RadishNotepad.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\RadishNotepad.exe"; Description: "Launch Radish Notepad"; Flags: nowait postinstall skipifsilent
