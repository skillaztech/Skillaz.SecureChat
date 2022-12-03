module Skillaz.SecureChat.Configuration

open FSharp.Configuration

type AppSettings = YamlConfig<FilePath="appsettings.yaml">
type UserSettings =
    YamlConfig<
        YamlText=
            """
            UserId: 00000000-0000-0000-0000-000000000000
            Name: ""
            SecretCode: 000000
            """>

