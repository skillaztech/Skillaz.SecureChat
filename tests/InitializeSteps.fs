module Skillaz.SecureChat.AcceptanceTests.InitializeSteps

open System
open System.IO
open Skillaz.SecureChat
open Skillaz.SecureChat.Chat
open Skillaz.SecureChat.IO.IOsDetector
open TickSpec
open Expecto

type InitializeSteps () =
    let mutable args = TestHelpers.emptyArgs
    let mutable model : Model option = None
    