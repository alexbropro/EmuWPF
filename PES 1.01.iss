#define MyAppName "ProjNimu"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "ProjNimu"
#define MyAppExeName "EmuWPF.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=installer
OutputBaseFilename=ProjNimu_Setup_v1.01
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application
Source: "EmuWPF\bin\publish\ProjEmu Versions 1.0\ProjEmu 1.01\EmuWPF.exe"; DestDir: "{app}"; Flags: ignoreversion

; Required DLLs

Source: "EmuWPF\bin\publish\ProjEmu Versions 1.0\ProjEmu 1.01\D3DCompiler_47_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "EmuWPF\bin\publish\ProjEmu Versions 1.0\ProjEmu 1.01\PenImc_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "EmuWPF\bin\publish\ProjEmu Versions 1.0\ProjEmu 1.01\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "EmuWPF\bin\publish\ProjEmu Versions 1.0\ProjEmu 1.01\vcruntime140_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "EmuWPF\bin\publish\ProjEmu Versions 1.0\ProjEmu 1.01\wpfgfx_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion

; ECores folder with all emulators
Source: "EmuWPF\bin\publish\ProjEmu Versions 1.0\ProjEmu 1.01\ECores\*"; DestDir: "{app}\ECores"; Flags: ignoreversion recursesubdirs createallsubdirs

; Games folder placeholder
Source: "Installer_Assets\placeholder.txt"; DestDir: "{app}\Games"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"