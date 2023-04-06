module Skillaz.SecureChat.ChatArgs

open Avalonia.Controls.ApplicationLifetimes
open Skillaz.SecureChat.Domain.Domain
open Skillaz.SecureChat.IConfigStorage
    
type ChatArgs = {
    ApplicationLifetime: IControlledApplicationLifetime
    ProcessDirectory: string
    AppSettings : AppSettings
    UserSettings : UserSettings
    UnixSocketsFolderPath : string
    UnixSocketFilePath : string
    ConfigStorage : IConfigStorage
}