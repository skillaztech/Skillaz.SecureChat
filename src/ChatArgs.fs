module Skillaz.SecureChat.ChatArgs

open Avalonia.Controls.ApplicationLifetimes
open Skillaz.SecureChat.IO.IOsDetector
    
type ChatArgs = {
    ApplicationLifetime: IControlledApplicationLifetime
    Version: string
    ProcessDirectory: string
    OsDetector: IOsDetector
}