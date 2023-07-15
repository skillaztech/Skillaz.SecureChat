module Skillaz.SecureChat.AcceptanceTests.SettingsSteps

open Expecto
open Skillaz.SecureChat
open Skillaz.SecureChat.AcceptanceTests.TestConfigStorage
open Skillaz.SecureChat.Chat
open Skillaz.SecureChat.ChatArgs
open Skillaz.SecureChat.Domain.Domain
open TickSpec

type SettingsSteps () =
    
    let startAppWith (args: ChatArgs) us =
        let configStorage = TestConfigStorage(us)
        let args = {
            args with
                UserSettings = us
                ConfigStorage = configStorage
        }
        Chat.init args |> fst, configStorage
    
    [<Given>]
    member _.``started application instance with secret code (.*)`` (secretCode: SecretCode) =
        let args = ArgsBuilder.mkArgs
        let us = { args.UserSettings with SecretCode = secretCode }
        startAppWith args us
        
    [<Given>]
    member _.``started application instance with username '(.*)'`` (userName: UserName) =
        let args = ArgsBuilder.mkArgs
        let us = { args.UserSettings with Name = userName }
        startAppWith args us
    
    [<Given>]
    member _.``secret code set by user is (.*)`` (secretCode: SecretCode) (model: Model, configStorage: TestConfigStorage) =
        { model with SecretCode = secretCode }, configStorage
    
    
    [<Given>]
    member _.``username set by user is (.*)`` (userName: UserName) (model: Model, configStorage: TestConfigStorage) =
        { model with UserName = userName }, configStorage
    
    [<When>]
    member _.``user saves user settings`` (model: Model, configStorage: TestConfigStorage) =
        Chat.update SaveUserSettingsToConfig model |> ignore
        configStorage
    
    [<Then>]
    member _.``saved secret code should be equal to (.*)`` (secretCode: SecretCode) (configStorage: TestConfigStorage) =
        Expect.equal configStorage.UserSettings.SecretCode secretCode "Secret code is not equal to expected"
    
    [<Then>]
    member _.``saved username should be equal to (.*)`` (userName: UserName) (configStorage: TestConfigStorage) =
        Expect.equal configStorage.UserSettings.Name userName "Username is not equal to expected"