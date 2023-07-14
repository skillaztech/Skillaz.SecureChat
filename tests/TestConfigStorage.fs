module Skillaz.SecureChat.AcceptanceTests.TestConfigStorage

open Skillaz.SecureChat.Domain.Domain
open Skillaz.SecureChat.IConfigStorage

type TestConfigStorage(userSettings: UserSettings) =
    member val UserSettings: UserSettings = userSettings with get, set
    
    interface IConfigStorage with
        member this.SaveUserSettings(userSettings) =
            this.UserSettings <- userSettings