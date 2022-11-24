module Skillaz.SecureChat.List

let appendNoDuplicate element list =
    match list |> List.contains element with
    | true -> list
    | false -> element :: list

let appendIf predicate element list =
    if predicate
    then element :: list
    else list

let exceptIf predicate excepted list =
    if predicate
    then list |> List.except excepted
    else list

let replaceByKey selector element list =
    list |> List.map (fun o -> if selector o then element else o)
    
let upsert selector element list =
    let f = list |> List.tryFind selector
    match f with
    | Some a -> replaceByKey selector element list
    | None -> element :: list

let doesNotContains element list =
    list |> List.contains element |> not