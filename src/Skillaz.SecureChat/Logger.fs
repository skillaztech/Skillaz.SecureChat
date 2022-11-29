module Skillaz.SecureChat.Logger

open Avalonia.Logging

let warnLogger = Logger.TryGet(LogEventLevel.Warning, "LOG-WARN-APP").Value