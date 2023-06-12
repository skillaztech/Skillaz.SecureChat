module Skillaz.SecureChat.IConfigStorage

open Skillaz.SecureChat.Domain.Domain

type IConfigStorage =
    abstract member SaveUserSettings : UserSettings -> unit