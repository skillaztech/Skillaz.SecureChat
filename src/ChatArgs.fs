module Skillaz.SecureChat.ChatArgs

open Skillaz.SecureChat.IO.IOsDetector
    
type ChatArgs = {
    ProcessDirectory: string
    OsDetector: IOsDetector
}