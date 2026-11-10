; Instalador do NofyMe (Inno Setup 6)
; Instala por usuário, sem exigir administrador. Mantém os dados em %APPDATA%\NofyMe.

#define AppName "NofyMe"
#define AppVersion "1.1.3"
#define AppExe "NofyMe.exe"

[Setup]
AppId={{D4F1A7B2-3C9E-4A18-9B6D-5E2F7C8A1D03}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=NofyMe
DefaultDirName={localappdata}\Programs\NofyMe
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=NofyMe-Setup-{#AppVersion}
SetupIconFile=..\Icone\notifica.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
MinVersion=10.0.17763

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Atalhos:"

[Files]
Source: "..\dist\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Abrir o {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; nada de %APPDATA%\NofyMe: os lembretes do usuário ficam preservados

[Code]
// Entradas de autostart antigas apontavam só para o .exe. Na atualização,
// acrescenta --startup para o app abrir em segundo plano, sem mostrar a janela.
procedure MigrateStartupEntry();
var
  Value: String;
begin
  if RegQueryStringValue(HKCU,
    'Software\Microsoft\Windows\CurrentVersion\Run', 'NofyMe', Value) then
  begin
    if Pos('--startup', Value) = 0 then
      RegWriteStringValue(HKCU,
        'Software\Microsoft\Windows\CurrentVersion\Run', 'NofyMe',
        '"' + ExpandConstant('{app}\NofyMe.exe') + '" --startup');
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    MigrateStartupEntry();
end;
