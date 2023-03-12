module Skillaz.SecureChat.AcceptanceTests.InitializeSteps

open Skillaz.SecureChat
open Skillaz.SecureChat.Chat
open TickSpec
open Expecto

type InitializeSteps () =
    
    let mutable model : Model = TestHelpers.emptyModel
     
    [<When>]
    member _.``application starts`` () =
        let afterInitModel, _ = Chat.init ""
        model <- afterInitModel
        
    [<Then>]
    member _.``user id should be not empty`` () =
        Expect.isNotEmpty model.UserId
        
    [<Then>]
    member _.``user name should be not empty`` () =
        Expect.isNotEmpty model.UserName
        
    [<Then>]
    member _.``secret code should be not empty`` () =
        Expect.notEqual model.SecretCode 0