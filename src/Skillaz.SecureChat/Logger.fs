module Skillaz.SecureChat.Logger

open System
open System.IO
open NLog
open NLog.FSharp

let private logsDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/ssc/", "/logs/", "ssc-${shortdate}.log")
             
let nlogger =
    let config = NLog.Config.LoggingConfiguration()
    
    let fileTarget = new NLog.Targets.FileTarget "file"
    fileTarget.Layout <- "${longdate} ${level} ${message:withexception=true}"
    fileTarget.FileName <- logsDirectory
    fileTarget.MaxArchiveFiles <- 3
    config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget)
    
    let consoleTarget = new NLog.Targets.ColoredConsoleTarget "console"
    config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget)
    
    LogManager.Configuration <- config
    Logger()