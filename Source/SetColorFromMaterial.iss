#ifndef BuildDir
  #define BuildDir "bin\Release\net481"
#endif

#define SourceRoot AddBackslash(SourcePath)
#define Payload SourceRoot + BuildDir + "\"

#if !FileExists(Payload + "SetColorFromMaterial.dll")
  #error Build the add-on first: dotnet build SetColorFromMaterial.vbproj -c Release
#endif

#define MyAppName "Set Color from Material"
#define MyAppVersion GetFileVersion(Payload + "SetColorFromMaterial.dll")
#define MyAppPublisher "Cator and the Alibre Design forum community"
#define MyAppURL "https://www.alibre.com/forum/index.php?resources/set-color-from-material.150/"
#define MyAppDescription "Host for the Set Color from Material Alibre Script program. This is a script that Cator wrote and others helped finalize - thanks for everyone's help - that will add a color when assigning material to your model. It has colors listed (RGB) for all the standard Alibre materials as well as colors that follow the KeyShot scheme. You may need to edit for your particular usage. Source: https://www.alibre.com/forum/index.php?resources/set-color-from-material.150/"
#define AddOnIdentifier "{{14D634F7-663E-4CEC-9B55-120BBA5066DA}"

[Setup]
AppId={{2C20C5B0-C78D-46AC-9AE9-59FE7897E207}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppComments={#MyAppDescription}
DefaultDirName={commonappdata}\Alibre AddOns\SetColorFromMaterial
LicenseFile={#SourceRoot}..\LICENSE
DisableProgramGroupPage=yes
OutputDir=installer
OutputBaseFilename=SetColorFromMaterial-setup-v{#MyAppVersion}
SetupIconFile={#Payload}SetColorFromMaterial.ico
UninstallDisplayIcon={app}\SetColorFromMaterial.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
MinVersion=6.1sp1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#Payload}SetColorFromMaterial.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}SetColorFromMaterial.adc"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}SetColorFromMaterial.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}IronPython*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}Microsoft.Scripting*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}Microsoft.Dynamic.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}scripts\*"; DestDir: "{app}\scripts"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceRoot}..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceRoot}..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKLM; Subkey: "SOFTWARE\Alibre Design Add-Ons"; ValueType: string; ValueName: "{#AddOnIdentifier}"; ValueData: "{app}"; Flags: uninsdeletevalue

[UninstallDelete]
Type: dirifempty; Name: "{app}\scripts\library"
Type: dirifempty; Name: "{app}\scripts"
Type: dirifempty; Name: "{app}"

[Code]
function AlibreIsRunning(): Boolean;
var
  Locator, Service, Processes: Variant;
begin
  Result := False;
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('', 'root\CIMV2');
    Processes := Service.ExecQuery('SELECT Name FROM Win32_Process WHERE Name = "Alibre Design.exe"');
    Result := Processes.Count > 0;
  except
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if AlibreIsRunning() then
  begin
    MsgBox('Close Alibre Design, then run this installer again.', mbError, MB_OK);
    Result := False;
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  if AlibreIsRunning() then
  begin
    MsgBox('Close Alibre Design, then uninstall again.', mbError, MB_OK);
    Result := False;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    MsgBox('Installed. Start Alibre Design, open a part, then choose'#13#10 +
           'Add-Ons > Set Color from Material.', mbInformation, MB_OK);
end;
