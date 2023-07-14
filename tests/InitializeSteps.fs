module Skillaz.SecureChat.AcceptanceTests.InitializeSteps

open System
open Skillaz.SecureChat
open Skillaz.SecureChat.ChatArgs
open TickSpec
open Expecto

type InitializeSteps () =
    
    [<Given>]
    member _.``default configuration`` () =
        ArgsBuilder.mkArgs
    
    [<When>]
    member _.``application starts`` (args: ChatArgs) =
        try
            Chat.init args |> ignore
            None
        with
        | e -> Some e
        
    [<Then>]
    member _.``no errors occurs`` (exn: Exception option) =
        Expect.isNone exn "No exception should be thrown"