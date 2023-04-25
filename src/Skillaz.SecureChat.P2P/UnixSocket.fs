namespace Skillaz.SecureChat.P2P

open System
open System.IO
open System.Net.Sockets
open System.Security.AccessControl
open System.Security.Principal
open Mono.Unix

module UnixSocket =
    
    let listener =
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        socket.SendTimeout <- P2PNetwork.defaultSocketTimeoutMs
        socket
        
    let tryBindTo (socketFilePath: string) (socket: Socket) =
        let directoryPath = Path.GetDirectoryName(socketFilePath)
        
        let isUnix = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
        
        let directoryWasExisting = Directory.Exists(directoryPath)
        let directory = Directory.CreateDirectory(directoryPath)
        
        if not directoryWasExisting
        then
            if isUnix
                then
                    let directoryInfo = UnixDirectoryInfo(directoryPath)
                    directoryInfo.FileAccessPermissions <- FileAccessPermissions.AllPermissions
                    directoryInfo.Refresh()
                else
                    let accessControl = directory.GetAccessControl()
                    accessControl.AddAccessRule(FileSystemAccessRule(SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit ||| InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    directory.SetAccessControl(accessControl)
        
        socket.Bind(UnixDomainSocketEndPoint(socketFilePath))
        
        if isUnix
        then
            let fileInfo = UnixFileInfo(socketFilePath)
            fileInfo.FileAccessPermissions <- FileAccessPermissions.AllPermissions
            fileInfo.Refresh()
        else
            let fileInfo = FileInfo(socketFilePath)
            let fileAccessControl = fileInfo.GetAccessControl()
            fileAccessControl.AddAccessRule(FileSystemAccessRule(SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            fileInfo.SetAccessControl(fileAccessControl)
        
    let client =
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        socket.SendTimeout <- P2PNetwork.defaultSocketTimeoutMs
        socket
        
    let connectSocket socket path  =
        P2PNetwork.connectSocket socket <| UnixDomainSocketEndPoint(path)