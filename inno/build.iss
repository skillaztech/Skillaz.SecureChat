﻿; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define AppId "3D86CA30-41C8-4E19-AA6A-1C20B6E6127D"
#define AuthorName "Danila Chervonny"
#define ApplicationName "Skillaz Secure Chat"
#define ApplicationInstallationFolder "SkillazSecureChat"
#define ApplicationExeFile "Skillaz.SecureChat.exe"
#define ApplicationVersion GetFileVersion("..\chat\bin\Release\{ApplicationExeFile}")

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={#AppId}
AppName={#ApplicationName}
AppVersion={#ApplicationVersion}
AppVerName={#ApplicationName} {#ApplicationVersion}
AppPublisher={#AuthorName}
DefaultDirName={localappdata}\{#ApplicationName}
DefaultGroupName={#ApplicationName}
AllowNoIcons=yes
OutputBaseFilename={#ApplicationName} Installer
Compression=lzma
SolidCompression=yes
AppCopyright={#AuthorName}
PrivilegesRequired=none
AppContact=contact@picolino.dev
SetupIconFile=..\logo.ico
VersionInfoVersion={#ApplicationVersion}
VersionInfoCopyright={#AuthorName}
VersionInfoProductName={#ApplicationName}
AppPublisherURL=https://picolino.dev/
AppSupportURL=https://picolino.dev/
VersionInfoProductVersion={#ApplicationVersion}
LicenseFile=..\license.md
UninstallDisplayName={#ApplicationName}
UninstallDisplayIcon={app}\logo.ico

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "..\chat\bin\Release\net6.0\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\chat\bin\Release\net6.0\*.json"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\chat\bin\Release\net6.0\*.exe"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\license.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#ApplicationInstallationFolder}"; Filename: "{app}\{#ApplicationExeFile}"
Name: "{userdesktop}\{#ApplicationInstallationFolder}"; Filename: "{app}\{#ApplicationExeFile}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ApplicationExeFile}"; Description: "{cm:LaunchProgram,{#ApplicationName}}"; Flags: nowait postinstall skipifsilent

[Code]                    
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//    IsUpdate Functions Definition
//-----------------------------------------------------------------------------
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1';
function IsUpgrade: Boolean;
var
  Value: string;
begin
  Result := (RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', Value) or
    RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', Value)) and (Value <> '');
end;          
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//    Getting PC Name
//-----------------------------------------------------------------------------
function GetComputerName(lpBuffer: AnsiString; var nSize: DWORD): BOOL;
external 'GetComputerNameA@kernel32.dll';

function GetPCName: string;
var
  Size: Cardinal;
  buffer: AnsiString;
begin
  Size := 16;
  SetLength(buffer, Size);
  GetComputerName(buffer, Size);
  Result := buffer;
end;
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//    Guid Functions Definition
//-----------------------------------------------------------------------------
function CoCreateGuid(var Guid:TGuid):integer;
 external 'CoCreateGuid@ole32.dll stdcall';

function FormatGuid(Guid:TGuid):string;
begin
  result := Format('%.8x-%.4x-%.4x-%.2x-%.2x-%.2x-%.2x-%.2x-%.2x-%.2x-%.2x', [Guid.D1, Guid.D2, Guid.D3, Guid.D4[0], Guid.D4[1], Guid.D4[2], Guid.D4[3], Guid.D4[4], Guid.D4[5], Guid.D4[6], Guid.D4[7]]);
end;
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
//    Add Secret Field
//-----------------------------------------------------------------------------
const                      
 InputQueryPageID = 100;

var
AppConfigPage: TInputQueryWizardPage;
IsUpgradeCached: Boolean;

function InitializeSetup(): Boolean;
begin
  IsUpgradeCached := IsUpgrade;
  Result := True;
end;

procedure InitializeWizard;
begin
  AppConfigPage := CreateInputQueryPage(wpLicense,
    'Заполните конфигурацию', 'Пожалуйста, заполните конфигурацию приложения',
    '');                 
  AppConfigPage.Add('Наименование клиента:', False);
  AppConfigPage.Values[0] := GetPCName()
  AppConfigPage.Add('Сохраните к себе секретный код или введите ключ другого клиента для связи:', False); 
  AppConfigPage.Values[1] := IntToStr(Random(1000000));
  AppConfigPage.Add('Выберите TCP/UDP порт для входа в сеть (он должен быть доступен и совпадать с портами других клиентов для корректного подключения):', False);
  AppConfigPage.Values[2] := '63211' 
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := (PageID = AppConfigPage.ID) and IsUpgrade;
end;

function FileReplaceString(FilePath: string; WhatReplace: string; ReplaceString: string):boolean;
var
  MyFile : TStrings;
  MyText : string;
begin
  Log('Replacing in file');
  MyFile := TStringList.Create;
  try
    Result := true;
    try
      MyFile.LoadFromFile(ExpandConstant('{app}' + FilePath));
      Log('File loaded');
      MyText := MyFile.Text;
      { Only save if text has been changed. }
      if StringChangeEx(MyText, WhatReplace, ReplaceString, True) > 0 then
      begin;
        Log('Inserted');
        MyFile.Text := MyText;
        MyFile.SaveToFile(ExpandConstant('{app}' + FilePath));
        Log('File saved');
      end;
    except
      Result := false;
    end;
  finally
    MyFile.Free;
  end;
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and (not IsUpgradeCached)  then
  begin
    Log('File installed, replacing...');            
    FileReplaceString('\appsettings.template.json', '{MachineName}', AppConfigPage.Values[0]);
    FileReplaceString('\appsettings.template.json', '{SecretCode}', AppConfigPage.Values[1]);
    FileReplaceString('\appsettings.template.json', '{ListenerPort}', AppConfigPage.Values[2]);
    FileReplaceString('\appsettings.template.json', '{ClientPort}', AppConfigPage.Values[2]);
    DeleteFile(ExpandConstant('{app}\appsettings.json'));
    RenameFile(ExpandConstant('{app}\appsettings.template.json'), ExpandConstant('{app}\appsettings.json'));
  end;
end;
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------