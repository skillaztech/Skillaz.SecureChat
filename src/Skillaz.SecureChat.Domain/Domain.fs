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
        LogLevel : string
    }

    type UserSettings = {
        UserId: UserId
        Name: UserName
        SecretCode: SecretCode
    }
    
    type RetranslationInfo = {
        RetranslatedBy: UserId list
    }
    
    /// Chat message 
    type AliveMessage = {
        /// Alive message sender user id
        UserId: UserId
        /// Alive message sender user name
        MessageSender: UserName
        /// Alive message sender secret code
        SecretCode: SecretCode
        /// Alive message retranslation info
        RetranslationInfo: RetranslationInfo
    }
    
    /// Chat message 
    type ChatMessage =
        {
            /// Chat message sender user id
            UserId: UserId
            /// Chat message sender user name
            MessageSender: UserName
            /// Chat message sender secret code
            SecretCode: SecretCode
            /// Chat message retranslation info
            RetranslationInfo: RetranslationInfo
            /// Chat message sending date and time
            DateTime: DateTime
            /// Chat message text
            MessageText: string
        }
        
        /// Rewrite comparison function required for messages deduplication
        member this.GetHalfHashCode() = hash <| this.MessageText + this.UserId + this.DateTime.Ticks.ToString() + this.SecretCode.ToString()
