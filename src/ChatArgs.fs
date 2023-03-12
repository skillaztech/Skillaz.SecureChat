module Skillaz.SecureChat.ChatArgs

open Skillaz.SecureChat.IO.OsDetector
    
type ChatArgs = {
    ProcessDirectory: string
    OsDetector: IOsDetector
}