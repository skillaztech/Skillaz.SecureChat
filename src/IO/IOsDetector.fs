module Skillaz.SecureChat.IO.IOsDetector

type IOsDetector =
    abstract IsLinux : unit -> bool
    abstract IsMacOS : unit -> bool