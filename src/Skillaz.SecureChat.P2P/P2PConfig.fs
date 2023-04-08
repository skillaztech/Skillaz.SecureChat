module Skillaz.SecureChat.P2P.P2PConfig

open Skillaz.SecureChat.P2P.P2Primitives

type LocalNetworkConfig = {
    UnixSocketsSharedFolderPath : DirectoryPath
    UnixSocketFileName : FilePath
}

type RemoteNetworkConfig = {
    ListenerPort : TcpPort
}

type P2PConfig = {
    LocalNetworkConfig : LocalNetworkConfig
    RemoteNetworkConfig : RemoteNetworkConfig
}