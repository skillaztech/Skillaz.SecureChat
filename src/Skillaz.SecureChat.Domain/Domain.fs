namespace Skillaz.SecureChat.Domain

open System
open System.Net

module Domain =
    type UserId = string
    type UserName = string
    type SecretCode = int

    type AppSettings = {
        MaxChatMessageLength : int
        ListenerTcpPort : int
        ClientTcpPort : int
        KnownRemotePeers : IPEndPoint list
    }

    type UserSettings = {
        UserId: UserId
        Name: UserName
        SecretCode: SecretCode
    }
    
    type RetranslationInfo = {
        RetranslatedBy: UserId list
    }
    
    type AliveMessage = {
        SenderUserId: UserId
        [<Obsolete("Old field")>] UserId: string
        SenderUserName: UserName
        [<Obsolete("Old field")>] MessageSender: string
        SecretCode: SecretCode
        RetranslationInfo: RetranslationInfo
    }
    
    type ChatMessage =
        {
            SenderUserId: UserId
            [<Obsolete("Old field")>] UserId: string
            SenderUserName: UserName
            [<Obsolete("Old field")>] MessageSender: string
            SecretCode: SecretCode
            RetranslationInfo: RetranslationInfo
            SendingDateTime: DateTime
            [<Obsolete("Old field")>] DateTime: DateTime
            MessageText: string
        }
        
        member this.GetHalfHashCode() = hash <| this.MessageText + this.SenderUserId + this.SendingDateTime.Ticks.ToString() + this.SecretCode.ToString()
