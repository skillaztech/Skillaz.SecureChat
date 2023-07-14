module Skillaz.SecureChat.AcceptanceTests.SharedSteps

open System
open Skillaz.SecureChat
open Skillaz.SecureChat.ChatArgs
open TickSpec
open Expecto

type SharedSteps () =
    
    [<Given>]
    member _.``default application configuration`` () =
        ArgsBuilder.mkArgs
    
    [<Given>]
    member _.``started application instance`` () =
        Chat.init ArgsBuilder.mkArgs |> fst