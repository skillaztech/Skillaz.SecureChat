namespace Skillaz.SecureChat

open System.IO
open System.Net.Sockets
open System.Security.AccessControl
open System.Security.Principal
open Skillaz.SecureChat.P2PNetwork

module UnixSocket =
    
    let listener (path:string) =
        let directory = Directory.CreateDirectory(Path.GetDirectoryName(path))
        
        let accessControl = directory.GetAccessControl()
        accessControl.AddAccessRule(FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit ||| InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
        directory.SetAccessControl(accessControl)
        
        File.Delete(path)
        
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        socket.Bind(UnixDomainSocketEndPoint(path))
        
        let fileInfo = FileInfo(path)
        let fileAccessControl = fileInfo.GetAccessControl()
        fileAccessControl.AddAccessRule(FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
        fileInfo.SetAccessControl(fileAccessControl)

        socket
        
    let client path =
        let socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP)
        connectSocket socket <| UnixDomainSocketEndPoint(path)