module Skillaz.SecureChat.INetworkProvider

open System.Net
open System.Net.Sockets
    
type INetworkListener =
    abstract member Socket : Socket // TODO: Hide socket under this abstraction to make it fakeable.
    abstract member IsBound : bool
    abstract member StartListen : unit -> unit
    abstract member Bind : unit -> unit
    

type INetworkProvider =
    abstract member RemoteListener : INetworkListener
    abstract member RemoteClientGenerateOnPort : int -> Socket
    abstract member RemoteClientConnect : IPAddress -> int -> Socket -> Socket
    abstract member LocalListener : INetworkListener
    abstract member LocalClientGenerate : unit -> Socket
    abstract member LocalClientConnect : Socket -> string -> Socket
    