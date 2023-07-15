module Skillaz.SecureChat.INetworkProvider

open System.Net
open System.Net.Sockets

type ClientType =
    | UnixSocket
    | Tcp of int

type ConnectionType =
    | UnixSocket of string
    | Tcp of IPEndPoint
    
type INetworkListener =
    abstract member Socket : Socket // TODO: Hide sockets under this abstraction to make it fakeable.
    abstract member IsBound : bool
    abstract member StartListen : unit -> unit
    abstract member Bind : unit -> unit
    abstract member GenerateClient : ClientType -> Socket
    abstract member Connect : ConnectionType -> Socket -> Socket

type INetworkProvider =
    abstract member RemoteListener : INetworkListener
    abstract member LocalListener : INetworkListener
    