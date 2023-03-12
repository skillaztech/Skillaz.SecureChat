module Skillaz.SecureChat.IO.OsDetector

type IOsDetector =
    abstract IsLinux : unit -> bool
    abstract IsMacOs : unit -> bool