# Prepare installers

## Windows

### Prerequirements
- [InnoSetup](https://jrsoftware.org/isinfo.php)

### Create .exe installer
1. Launch InnoSetup
2. Compile windows-installer/installer.iss to produce installer .exe file
3. Send .exe file to target machine

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

### Prepare .app package

1. Firstly, prepare binaries for publishing on target machine:
    ```shell
    dotnet publish -c Release -r osx-x64
    ```

2. Next move content of generated `osx-x64` folder to `osx-installer/ssc.app/Contents/MacOS/osx-x64` folder.
3. If you created the .app on Windows, make sure to run `chmod +x osx-installer/ssc.app/Contents/MacOS/Skillaz.SecureChat` from a Unix machine. Otherwise, the app will not start on macOS.

You may also check official instruction how to build and distribute .app packages by Avalonia:
[link](https://docs.avaloniaui.net/docs/distribution-publishing/macos)
