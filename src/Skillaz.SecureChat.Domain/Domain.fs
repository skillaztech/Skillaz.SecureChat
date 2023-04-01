namespace Skillaz.SecureChat.Domain

open System

module Domain =
    type UserId = string
    type UserName = string
    type SecretCode = int
    
    type RetranslationInfo = {
        RetranslatedBy: UserId list
    }
    
    type AliveMessage = {
        SenderUserId: UserId
        SenderUserName: UserName
        SecretCode: SecretCode
        RetranslationInfo: RetranslationInfo
    }
    
    type ChatMessage =
        {
            SenderUserId: UserId
            SenderUserName: UserName
            SecretCode: SecretCode
            RetranslationInfo: RetranslationInfo
            SendingDateTime: DateTime
            MessageText: string
        }
        
        member this.GetHalfHashCode() = hash <| this.MessageText + this.SenderUserId + this.SendingDateTime.Ticks.ToString() + this.SecretCode.ToString()
