<img src="logo.ico" alt="logo" align="right"/>

# Skillaz Secure Chat

Peer-to-peer chat application for network communication with ability to setup data transmission limitations, separated groups and multiple-users-on-one-machine support.

[![Build and Deploy](https://github.com/skillaztech/Skillaz.SecureChat/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/skillaztech/Skillaz.SecureChat/actions/workflows/build-and-publish.yml)
![Supported platforms](https://img.shields.io/badge/platforms-win--x64%20%7C%20osx--x64%20%7C%20ubuntu--x64-blue)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/skillaztech/skillaz.securechat)
![GitHub all releases](https://img.shields.io/github/downloads/skillaztech/skillaz.securechat/total?color=blue)

## Problem
Imagine, you have a single terminal server that you use to access some protected environment, for example, RDP-server. 
Also you have a lot of sensitive data under that protected environment. In common case limited access to that information is allowed and needed,
but if a big bunch of data would be exported from protected environment it will be fatal.

RDP server allows you to setup data in- and out- transmission rules, but it has no flexibility in these settings. 
You can't allow send X bytes of data from RDP server, you may only block or allow it. **And this behaviour does not match our requirements**.

## Solution
Skillaz Secure Chat (SSC) allows you to setup communication between RDP server and client machines through network with flexible limitations.

For now, SSC able to setup these limitations:
- Message length (symbols count)
- *TBD...*

<p align="center" width="100%">
    <img src="https://user-images.githubusercontent.com/17460456/221876159-cb8bdd9f-986d-4321-9871-108e644a3f26.png" alt="ssc-screenshot"/>
</p>

## Quick start

1. Download [latest release installer](https://github.com/skillaztech/Skillaz.SecureChat/releases/latest) for your operation system
2. Install on machines you want to setup communication between (see installation sections for different operation systems below)
3. Open specific TCP port on machine, where you want to access to (Default: `63211`).
4. Go to app installation folder and open `appsettings.yaml`, fill up `KnownPeers` section with IP addresses of machines, you installed SSC into.
   - Also you may setup other parameters to fit into your requirements
5. Launch SSC on both machines.

### Secret codes
To setup communications between two machines you need to make security code that was randomly generated at the first application launch to match onto both applications you want to connect.
If you have `123456` secret code on the first machine, you should set equal secret code on the second machine. Then they will be connected and you could see it in list of connected users.

This allows to create groups of users that able to move from one group to another by changing their secret code if they are located in same network.

### Logs
Default logs directories:

Linux: `/home/<username>/.local/share/ssc/logs`  
Windows: `C:\Users\<username>\AppData\Local\ssc\logs`

# Launch installers

## Windows
1. Launch .exe installer
2. Go through installation wizard till the end
3. Launch installed application

## Linux
1. On target machine execute this:
    ```shell
    sudo dpkg -i <path to .deb file on target machine>
    ```

2. To show this app into applications menu you need to execute:
    ```shell
    sudo cp /usr/share/Skillaz.SecureChat/ubuntu-installer/ssc.desktop /usr/share/applications/ssc.desktop
    ```

## MacOS
1. Open .dmg package on target machine
2. Move SSC app to Applications folder
3. Because unsinged apps are blocked by macOS we should disable quarantine for downloaded app:
    ```shell
    sudo xattr -d com.apple.quarantine "/Applications/SSC.app"
    ```
4. Launch SSC

<details>
 <summary><h3>Manual Build Installers</h3></summary>
 
## Windows

### Prerequirements
- [Inno Script Studio](https://www.kymoto.org/products/inno-script-studio) or [InnoSetup](https://jrsoftware.org/isinfo.php)

### Create .exe installer
1. Prepare binaries by this command:
    ```shell
    dotnet publish -c Release -r win-x64 --self-contained
    ```
2. Launch Inno Script Studio
3. Compile windows-installer/installer.iss to produce installer .exe file
4. Send .exe file to target machine

## Linux DEB package
### Prerequirements
- [dotnet deb](https://github.com/quamotion/dotnet-packaging)
    ```shell
    dotnet tool install --global dotnet-deb
    dotnet deb install
    ```

### Create .deb package
1. Execute this to create deb package:
    ```shell
    dotnet deb -r ubuntu-x64 -c Release
    ```

2. Next move this package to target machine:
    ```shell
    scp -i <path to pkey file for ssh authorization> <path to .deb> <remote login>@<target machine remote address>:<target file path .deb file>
    ```

## MacOS

### Prerequirements

- Macbook (or other OSX machine)
- [create-dmg utility](https://github.com/create-dmg/create-dmg)

### Prepare .app package

1. Firstly, prepare binaries for publishing on target machine:
    ```shell
    dotnet publish -c Release -r osx-x64 --self-contained
    ```

2. Next move content of generated `osx-x64` folder to `osx-installer/SSC.app/Contents/MacOS/osx-x64` folder.
3. If you created the .app on Windows, make sure to run `chmod +x osx-installer/SSC.app/Contents/MacOS/Skillaz.SecureChat` from a Unix machine. Otherwise, the app will not start on macOS.
4. Create dmg package this way:
   ```shell
   create-dmg \
   --volname "Skillaz Secure Chat" \
   --volicon "src/osx-installer/SSC.app/Contents/Resources/logo.icns" \
   --icon "SSC.app" 30 60 \
   --hide-extension "SSC.app" \
   --app-drop-link 280 60 \
   --no-internet-enable \
   "Skillaz.SecureChat.osx-x64.dmg" \
   "src/osx-installer/SSC.app"
   ```
5. Next, launch this command to setup external volume icon:
   ```shell
   src/osx-installer/set-ex-icon src/osx-installer/SSC.app/Contents/Resources/logo.icns Skillaz.SecureChat.osx-x64.dmg
   ```
6. Distribute this dmg package to target users

You may also check official instruction how to build and distribute .app packages by Avalonia:
[link](https://docs.avaloniaui.net/docs/distribution-publishing/macos)
 
</details>
