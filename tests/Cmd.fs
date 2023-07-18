module Skillaz.SecureChat.AcceptanceTests.Cmd
    
let exec (cmds: Elmish.Cmd<'msg>) =
    cmds |> List.iter (fun call -> call (fun m -> ()))

let execd dispatcher (cmds: Elmish.Cmd<'msg>) =
    cmds |> List.iter (fun call -> call dispatcher)

// Execute and return model
let execm (model, cmds: Elmish.Cmd<'msg>) =
    execd (fun m -> ()) cmds
    model

// Execute and return model
let execdm dispatcher (model, cmds: Elmish.Cmd<'msg>) =
    execd dispatcher cmds
    model
    
// Execute and ignore model
let execim (model, cmds: Elmish.Cmd<'msg>) =
    execdm (fun m -> ()) (model, cmds) |> ignore
    
// Execute and ignore model
let execdim dispatcher (model, cmds: Elmish.Cmd<'msg>) =
    execdm dispatcher (model, cmds) |> ignore