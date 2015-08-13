[Setup]
AppName=BzAB
AppVersion=0.1.0.3
DefaultDirName={localappdata}\BzAB\App
DefaultGroupName=My Program
UninstallDisplayIcon={app}\BzAB.exe
Compression=lzma2
SolidCompression=yes
OutputDir="."
DisableDirPage=yes
DisableFinishedPage=yes
DisableProgramGroupPage=yes
DisableWelcomePage=yes
OutputBaseFilename=BzABInst

[Files]
Source: "bin\Release\*.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\*.dll"; DestDir: "{app}"

[Icons]
Name: "{userstartup}\BzAB"; Filename: "{app}\BzAB.exe"

[Run]
Filename: "{app}\BzAB.exe"; Flags: postinstall nowait
