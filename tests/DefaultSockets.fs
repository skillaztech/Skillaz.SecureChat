module Skillaz.SecureChat.AcceptanceTests.DefaultSockets

open Skillaz.SecureChat.P2P

let remoteListener = UnixSocket.listener
let localListener = UnixSocket.listener