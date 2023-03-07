namespace Skillaz.SecureChat

open System
open System.IO
open System.Net.Sockets
open System.Security.AccessControl
open System.Security.Principal
open Mono.Unix
open Skillaz.SecureChat.P2PNetwork

module UnixSocket =
    
    let listener (path:string) =
        let directoryPath = Path.GetDirectoryName(path)
        
        let isUnix = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
        
        let directoryWasExisting = Directory.Exists(directoryPath)
        let directory = Directory.CreateDirectory(directoryPath)
        
        if not directoryWasExisting
        then
            if isUnix
                then
                    let directoryInfo = UnixDirectoryInfo(directoryPath)
                    directoryInfo.FileAccessPermissions <-FileAccessPermissions.AllPermissions
                    directoryInfo.Refresh()
                else
                    let accessControl = directory.GetAccessControl()
                    accessControl.AddAccessRule(FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit ||| InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    directory.SetAccessControl(accessControl)
        
        File.Delete(path)
        
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        socket.SendTimeout <- defaultSocketTimeoutMs
        socket.Bind(UnixDomainSocketEndPoint(path))
        
        if isUnix
        then
            let fileInfo = UnixFileInfo(path)
            fileInfo.FileAccessPermissions <-FileAccessPermissions.AllPermissions
            fileInfo.Refresh()
        else
            let fileInfo = FileInfo(path)
            let fileAccessControl = fileInfo.GetAccessControl()
            fileAccessControl.AddAccessRule(FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            fileInfo.SetAccessControl(fileAccessControl)

        socket
        
    let client path =
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        socket.SendTimeout <- defaultSocketTimeoutMs
        connectSocket socket <| UnixDomainSocketEndPoint(path)