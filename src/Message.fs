namespace Skillaz.SecureChat

open System

module Message =
    type RetranslationInfo = {
        RetranslatedBy: string list
    }
    
    type AliveMessage = {
        MessageSender: string
        SecretCode: int
        UserId: string
        RetranslationInfo: RetranslationInfo
    }
    
    type ChatMessage = {
        MessageText: string
        MessageSender: string
        DateTime: DateTime
        SecretCode: int
        UserId: string
        RetranslationInfo: RetranslationInfo
    }

