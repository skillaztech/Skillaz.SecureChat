namespace Skillaz.SecureChat

open FSharp.Data

module AppSettings =
    type AppSettingsJson = JsonProvider<"appsettings.json">
    
    let load (path:string) =
        AppSettingsJson.Load path