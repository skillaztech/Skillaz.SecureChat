﻿namespace Skillaz.SecureChat

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
        
        if isUnix
        then
            let directoryInfo = UnixDirectoryInfo(directoryPath)
            directoryInfo.FileAccessPermissions <-
                FileAccessPermissions.UserReadWriteExecute
                ||| FileAccessPermissions.GroupReadWriteExecute
                ||| FileAccessPermissions.OtherReadWriteExecute
            directoryInfo.Refresh()
        else
            let directory = Directory.CreateDirectory(directoryPath)
            let accessControl = directory.GetAccessControl()
            accessControl.AddAccessRule(FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit ||| InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            directory.SetAccessControl(accessControl)
        
        File.Delete(path)
        
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        socket.Bind(UnixDomainSocketEndPoint(path))
        
        if isUnix
        then
            let fileInfo = UnixFileInfo(path)
            fileInfo.FileAccessPermissions <-
                FileAccessPermissions.UserReadWriteExecute
                ||| FileAccessPermissions.GroupReadWriteExecute
                ||| FileAccessPermissions.OtherReadWriteExecute
            fileInfo.Refresh()
        else
            let fileInfo = FileInfo(path)
            let fileAccessControl = fileInfo.GetAccessControl()
            fileAccessControl.AddAccessRule(FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
            fileInfo.SetAccessControl(fileAccessControl)

        socket
        
    let client path =
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        connectSocket socket <| UnixDomainSocketEndPoint(path)