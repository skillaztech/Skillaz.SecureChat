module Skillaz.SecureChat.FileConfiguration

open FSharp.Configuration

type FileAppSettings = YamlConfig<FilePath="appsettings.yaml">
type FileUserSettings = YamlConfig<FilePath="usersettings.yaml">