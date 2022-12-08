<img src="logo.ico" alt="logo" align="right"/>

# Skillaz Secure Chat

Peer-to-peer chat application for network communication with ability to setup data transmission limitations, separated groups and multiple-users-on-one-machine support.

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

## Prerequirements

1. Open specific TCP port on machine, where you want to access to (Default: `63211`).
2. Install Skillaz Secure Chat
3. Setup `ClientTcpPort` and `ListenerTcpPort` into `appsettings.yaml` file to port, you've set in 1st step.
4. Setup limitations into `appsettings.yaml` to fit your requirements

## Quick start

1. Download installer for your operation system from releases page
2. Install on machines you want to setup communication between (see installation sections for different operation systems below)
3. Open specific TCP port on machine, where you want to access to (Default: `63211`).
4. Go to app installation folder and open `appsettings.yaml`, fill up `KnownPeers` section with IP addresses of machines, you installed SSC into.
   - Also you may setup other parameters to fit into your requirements
5. Launch SSC on both machines.

### Secret codes
To setup communications between two machines you need to make security code that was randomly generated at the first application launch to match onto both applications you want to connect.
If you have `123456` secret code on the first machine, you should set equal secret code on the second machine. Then they will be connected and you could see it in list of connected users.

This allows to create groups of users that able to move from one group to another by changing their secret code if they are located in same network.

# Build & Launch installers

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

### Installation
1. Launch .exe installer
2. Go through installation wizard till the end
3. Launch installed application

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

### Installation
1. On target machine execute this:
    ```shell
    sudo dpkg -i <path to .deb file on target machine>
    ```

2. To show this app into applications menu you need to execute:
    ```shell
    sudo cp /usr/share/Skillaz.SecureChat/ubuntu-installer/ssc.desktop /usr/share/applications/ssc.desktop
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
   --volicon "src/Skillaz.SecureChat/osx-installer/SSC.app/Contents/Resources/logo.icns" \
   --icon "SSC.app" 30 60 \
   --hide-extension "SSC.app" \
   --app-drop-link 280 60 \
   "Skillaz Secure Chat.dmg" \
   "src/Skillaz.SecureChat/osx-installer/SSC.app"
   ```
5. Distribute this dmg package to target users

You may also check official instruction how to build and distribute .app packages by Avalonia:
[link](https://docs.avaloniaui.net/docs/distribution-publishing/macos)

### Installation
1. Open .dmg package on target machine
2. Move SSC app to Applications folder
3. Launch SSC
