namespace Skillaz.SecureChat.AcceptanceTests.DefaultDispatchers

open Skillaz.SecureChat.Chat

module CollectorDispatcher =
    let mutable messages: Msg list = []
    let dispatcher msg =
        messages <- msg :: messages
    
    // type CollectorDispatcher() =
    //     member val Messages = [] with get,set
    //     member this.dispatcher msg =
    //         this.Messages <- msg :: this.Messages