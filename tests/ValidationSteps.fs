module Skillaz.SecureChat.AcceptanceTests.ValidationSteps

open System
open Expecto
open Skillaz.SecureChat
open Skillaz.SecureChat.Chat
open TickSpec

type ValidationSteps () =
    
    [<Given>]
    member _.``started instance`` () =
        Chat.init ArgsBuilder.mkArgs |> fst
        
    [<When>]
    member this.``user set username between (.*) and (.*) chars length`` (minSize: int) (maxSize: int) (model: Model) =
        let size = Random.Shared.Next(minSize, maxSize + 1)
        let userName = String(Array.init size (fun _ -> 'o'))
        let msg = UserNameChanged userName
        Chat.update msg model |> fst
        
    [<Then>]
    member _.``validation errors count should be (.*)`` (errorsCount: int) (model: Model) =
        Expect.hasLength model.UserNameValidationErrors errorsCount ""