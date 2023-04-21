﻿; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define AppId "3D86CA30-41C8-4E19-AA6A-1C20B6E6127D"
#define AuthorName "Danila Chervonny"
#define ApplicationName "Skillaz Secure Chat"
#define ApplicationShortenName "Skillaz.SecureChat"
#define ApplicationInstallationFolder "SkillazSecureChat"
#define ApplicationExeFile "Skillaz.SecureChat.exe" 
#define ApplicationVersionMajorPart
#define ApplicationVersionMinorPart
#define ApplicationVersionPatchPart
#define ApplicationVersionBuildPart
#expr ParseVersion("..\bin\Release\net6.0\win-x64\Skillaz.SecureChat.exe", ApplicationVersionMajorPart, ApplicationVersionMinorPart, ApplicationVersionPatchPart, ApplicationVersionBuildPart)
#define ApplicationVersion Str(ApplicationVersionMajorPart) + "." + Str(ApplicationVersionMinorPart) + "." + Str(ApplicationVersionPatchPart)

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={#AppId}
AppName={#ApplicationName}
AppVersion={#ApplicationVersion}
AppVerName={#ApplicationName} {#ApplicationVersion}
AppPublisher={#AuthorName}
DefaultDirName={commonpf}\{#ApplicationName}
DefaultGroupName={#ApplicationName}
AllowNoIcons=yes
OutputBaseFilename={#ApplicationShortenName}.{#ApplicationVersion}.win-x64
Compression=lzma
SolidCompression=yes
AppCopyright={#AuthorName}
PrivilegesRequired=none
AppContact=contact@picolino.dev
SetupIconFile=..\..\logo.ico
VersionInfoVersion={#ApplicationVersion}
VersionInfoCopyright={#AuthorName}
VersionInfoProductName={#ApplicationName}
AppPublisherURL=https://picolino.dev/
AppSupportURL=https://picolino.dev/
VersionInfoProductVersion={#ApplicationVersion}
UninstallDisplayName={#ApplicationName}
UninstallDisplayIcon={app}\logo.ico

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: "..\bin\Release\net6.0\win-x64\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\bin\Release\net6.0\win-x64\*.exe"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\..\logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#ApplicationInstallationFolder}"; Filename: "{app}\{#ApplicationExeFile}"
Name: "{userdesktop}\{#ApplicationInstallationFolder}"; Filename: "{app}\{#ApplicationExeFile}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ApplicationExeFile}"; Description: "{cm:LaunchProgram,{#ApplicationName}}"; Flags: nowait postinstall skipifsilent