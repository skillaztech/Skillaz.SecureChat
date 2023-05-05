module Skillaz.SecureChat.AcceptanceTests.InitializeSteps

open System
open Skillaz.SecureChat
open Skillaz.SecureChat.Chat
open Skillaz.SecureChat.ChatArgs
open TickSpec
open Expecto

type InitializeSteps () =
    let mutable args : ChatArgs option = None
    let mutable model : Model option = None
    let mutable exn : Exception option = None
    
    [<Given>]
    member _.``default configuration`` () =
        args <- Some TestHelpers.emptyArgs
    
    [<When>]
    member _.``application starts`` () =
        try
            let newModel, _ = Chat.init args.Value
            model <- Some newModel
        with
        | e ->
            exn <- Some e
        
    [<Then>]
    member _.``no errors occurs`` () =
        Expect.isNone exn