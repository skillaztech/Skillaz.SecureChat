namespace Skillaz.SecureChat.Domain

open System
open System.Net

module Domain =
    /// User unique identifier
    type UserId = string
    /// User name
    type UserName = string
    /// Secret code
    type SecretCode = int

    /// Application settings
    type AppSettings = {
        /// Maximum string length for message input
        MaxChatMessageLength : int
        /// Server listener tcp port
        ListenerTcpPort : int
        /// Client tcp port
        ClientTcpPort : int
        /// Remote peers used to connect to the network
        KnownRemotePeers : IPEndPoint list
        /// Log level [Trace;Debug;Info;Warning;Error;Fatal]
        LogLevel : string
    }

    /// User settings
    type UserSettings = {
        /// Unique user identifier
        UserId: UserId
        /// User-defined name
        Name: UserName
        /// Secret code to connect to user group
        SecretCode: SecretCode
    }
    
    /// Remote connected user
    type ConnectedUser = {
        /// Connected user name
        UserName: string
        /// Connected user identifier
        UserId: string
        /// If the current time goes beyond this date, then the user is considered disconnected.
        ConnectedTill: DateTime
    }
    
    /// Message retranslation information
    type RetranslationInfo = {
        /// Users whom bypass current message
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
