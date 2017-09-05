#load "tools/includes.fsx"

open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.Data")
        .VersionFrom("WebSharper")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun fw -> fw.Net45)

let main =
    bt.WebSharper4.Library("WebSharper.Data")
        .SourcesFromProject()
        .WithSourceMap()

        .References(fun r ->
            [ 
                r.NuGet("FSharp.Data").Version("[2.2.5]").ForceFoundVersion().Reference() 
            ])

let test =
    bt.WebSharper4.Library("WebSharper.Data.Test")
        .References(fun r ->
            [ 
                r.NuGet("FSharp.Data").Version("[2.2.5]").Reference() 
                r.NuGet("WebSharper.Reactive").Latest(true).Reference()
                r.NuGet("WebSharper.UI.Next").Latest(true).Reference()
                r.NuGet("WebSharper.ChartJs").Latest(true).Reference()
                r.NuGet("WebSharper.Charting").Latest(true).Reference()
                r.Project main
            ])

bt.Solution [
    main
    test

    bt.NuGet.CreatePackage()
        .Configure(fun configuration ->
            { configuration with
                Title = Some "WebSharper.Data"
                LicenseUrl = Some "http://websharper.com/licensing"
                ProjectUrl = Some "https://github.com/intellifactory/websharper.data"
                Description = "FSharp.Data proxies for WebSharper"
                Authors = [ "IntelliFactory" ]
                RequiresLicenseAcceptance = true })
        .Add(main)
]
|> bt.Dispatch
