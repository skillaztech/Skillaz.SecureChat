open System.Reflection
open Expecto

let assembly = Assembly.GetExecutingAssembly()
let stepDefinitions = TickSpec.StepDefinitions(assembly)

let featureFromEmbeddedResource (resourceName: string) : TickSpec.Feature =
    let stream = assembly.GetManifestResourceStream(resourceName)
    stepDefinitions.GenerateFeature(resourceName, stream)

let testListFromFeature (feature: TickSpec.Feature) : Expecto.Test =
    feature.Scenarios
    |> Seq.map (fun scenario -> testCase scenario.Name scenario.Action.Invoke)
    |> Seq.toList
    |> testList feature.Name
    |> testSequenced

let featureTest (resourceName: string) =
    (assembly.GetName().Name + "." + resourceName)
    |> featureFromEmbeddedResource
    |> testListFromFeature

[<Tests>]
let initializeTests = featureTest "Initialize.feature"
[<Tests>]
let validationTests = featureTest "Validation.feature"

[<EntryPoint>]
let main args =
    runTestsInAssemblyWithCLIArgs [] args