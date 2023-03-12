module Skillaz.SecureChat.AcceptanceTests.InitializeSteps

open System
open System.IO
open Skillaz.SecureChat
open Skillaz.SecureChat.Chat
open Skillaz.SecureChat.IO.OsDetector
open TickSpec
open Expecto

type InitializeSteps () =
    let mutable args = TestHelpers.emptyArgs
    let mutable model : Model option = None
    
    [<Given>]
    member _.``(.*) operation system`` os =
        let osDetector = {
            new IOsDetector with
            member _.IsLinux() = os = "Linux"
            member _.IsMacOs() = os = "MacOS"
        }
        
        args <- { args with OsDetector = osDetector }
     
    [<When>]
    member _.``application starts`` () =
        let afterInitModel, _ = Chat.init args
        model <- Some afterInitModel
        
    [<Then>]
    member _.``user id should be not empty`` () =
        Expect.isNotEmpty model.Value.UserId
        
    [<Then>]
    member _.``user name should be not empty`` () =
        Expect.isNotEmpty model.Value.UserName
        
    [<Then>]
    member _.``secret code should be not empty`` () =
        Expect.notEqual model.Value.SecretCode 0
    
    [<Then>]
    member _.``unix sockets folder should be (.*)`` folder =
        let folder =
            match folder with
            | "ProgramData" -> Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "/ssc/")
            | "tmp" -> "/tmp/ssc/"
            | _ -> failwithf $"Unsupported folder: %s{folder}"
        Expect.equal model.Value.UnixSocketFolder folder "Unix socket folder"