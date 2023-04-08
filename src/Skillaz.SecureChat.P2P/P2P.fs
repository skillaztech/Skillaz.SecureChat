module Skillaz.SecureChat.P2P

open System.Net
open Skillaz.SecureChat.P2P.P2Primitives

type Local = {
    tryListenLocal : FilePath -> Async<unit>
    isListeningLocal : unit -> bool
    tryConnectToLocalPeer : FilePath -> Async<unit>
}

type Remote = {
    tryListenRemote : IPAddress -> TcpPort -> Async<unit>
    isListeningRemote : unit -> bool
    tryConnectToRemotePeer : IPEndPoint -> Async<unit>
}

type P2PPeer = {
    Local : Local
    Remote : Remote
    sendPkg : byte[] -> Async<unit>
}