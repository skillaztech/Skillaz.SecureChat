module Skillaz.SecureChat.ChatArgs

open Skillaz.SecureChat.IO.IOsDetector
    
type ChatArgs = {
    Version: string
    ProcessDirectory: string
    OsDetector: IOsDetector
}