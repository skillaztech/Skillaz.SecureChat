module Skillaz.SecureChat.INetworkProvider

open System.Net
open System.Net.Sockets
    
type INetworkListener =
    abstract member Socket : Socket // TODO: Hide socket under this abstraction to make it fakeable.
    abstract member IsBound : bool
    abstract member StartListen : unit -> unit
    
type INetworkRemoteListener =
    inherit INetworkListener
    abstract member BindTo : IPEndPoint -> unit
    
type INetworkLocalListener =
    inherit INetworkListener
    abstract member BindTo : string -> unit
    

type INetworkProvider =
    abstract member RemoteListener : INetworkRemoteListener
    abstract member LocalListener : INetworkLocalListener
    