#load "tools/includes.fsx"

open IntelliFactory.Build

let bt =
    BuildTool().PackageId("Zafir.Data")
        .VersionFrom("Zafir")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun fw -> fw.Net45)

let main =
    bt.Zafir.Library("WebSharper.Data")
        .SourcesFromProject()
        .WithSourceMap()

        .References(fun r ->
            [ 
                r.NuGet("FSharp.Data").Version("[2.2.5]").ForceFoundVersion().Reference() 
            ])

let test =
    bt.Zafir.Library("WebSharper.Data.Test")
        .References(fun r ->
            [ 
                r.NuGet("FSharp.Data").Version("[2.2.5]").Reference() 
                r.NuGet("Zafir.Reactive").Latest(true).Reference()
                r.NuGet("Zafir.UI.Next").Latest(true).Reference()
                r.NuGet("Zafir.ChartJs").Latest(true).Reference()
                r.NuGet("Zafir.Charting").Latest(true).Reference()
                r.Project main
            ])

bt.Solution [
    main
    test

    bt.NuGet.CreatePackage()
        .Configure(fun configuration ->
            { configuration with
                Title = Some "Zafir.Data"
                LicenseUrl = Some "http://websharper.com/licensing"
                ProjectUrl = Some "https://github.com/intellifactory/websharper.data"
                Description = "FSharp.Data proxies for Zafir"
                Authors = [ "IntelliFactory" ]
                RequiresLicenseAcceptance = true })
        .Add(main)
]
|> bt.Dispatch
