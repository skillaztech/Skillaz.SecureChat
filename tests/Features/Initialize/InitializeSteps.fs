module Skillaz.SecureChat.AcceptanceTests.InitializeSteps

open System
open Elmish
open Skillaz.SecureChat
open Skillaz.SecureChat.ChatArgs
open TickSpec
open Expecto

type InitializeSteps () =
    
    [<When>]
    member _.``application starts`` (args: ChatArgs) =
        try
            Chat.init args |> Result.Ok
        with
        | e -> Result.Error e
        
    [<When>]
    member _.``initial commands executes`` (initResult: Result<Chat.Model * Cmd<Chat.Msg>, Exception>) =
        match initResult with
        | Ok (model, cmd) -> Cmd.execm (model, cmd)
        | Error e -> failtest <| e.ToString()
        
    [<Then>]
    member _.``remote listener is bounded`` () =
        Expect.isTrue DefaultSockets.remoteListener.IsBound
        
    [<Then>]
    member _.``local listener is bounded`` () =
        Expect.isTrue DefaultSockets.localListener.IsBound
        
    [<Then>]
    member _.``no errors occurs`` (initResult: Result<(Chat.Model * Cmd<Chat.Msg>), Exception>) =
        Expect.isOk initResult "Exception throws when app starts"