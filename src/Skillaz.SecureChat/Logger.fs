module Skillaz.SecureChat.Logger

open System
open System.IO
open NLog
open NLog.FSharp

let private logsDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "/ssc/", "/logs/", "ssc-${shortdate}.log")
             
let nlogger =
    let config = NLog.Config.LoggingConfiguration()
    
    let defaultLayout = "${longdate} ${level} ${message:withexception=true}"
    
    let fileTarget = new NLog.Targets.FileTarget "file"
    fileTarget.Layout <- defaultLayout
    fileTarget.FileName <- logsDirectory
    fileTarget.MaxArchiveFiles <- 3
    config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget)
    
    let debugTarget = new NLog.Targets.DebuggerTarget "debug"
    debugTarget.Layout <- defaultLayout
    config.AddRule(LogLevel.Debug, LogLevel.Fatal, debugTarget)
    
    LogManager.Configuration <- config
    Logger()