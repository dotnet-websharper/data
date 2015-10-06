namespace WebSharper.Data.Test

open WebSharper
//open WebSharper.UI.Next
//open WebSharper.UI.Next.Html
//open WebSharper.UI.Next.Client
//open WebSharper.Charting
open WebSharper.JavaScript

open FSharp.Data

[<JavaScript>]
module Client =    
    type WorldBank = WorldBankDataProvider<Asynchronous=true>
    let data = WorldBank.GetDataContext()

    (*let countries =
        [| data.Countries.Austria
           data.Countries.Hungary
           data.Countries.``United Kingdom``
           data.Countries.``United States`` |]

    let schoolEnrollment =
        countries
        |> Seq.map (fun c -> c.Indicators.``School enrollment, tertiary (% gross)``)
        |> Async.Parallel

    let mkData (i : Runtime.WorldBank.Indicator) =
        Seq.zip (Seq.map string i.Years) i.Values

    let randomColor =
        let rand = System.Random()
        fun () ->
            let r = rand.Next 256
            let g = rand.Next 256
            let b = rand.Next 256
            Color.Rgba(r,g,b,1.)

    let colors = 
        countries |> Array.map (fun _ -> randomColor ())

    let chart =
        let cfg = 
            ChartJs.LineChartConfiguration(
                PointDot = false,
                BezierCurve = true,
                DatasetFill = false)

        async {
            let! data = schoolEnrollment
            return
                data
                |> Array.map mkData
                |> Array.zip colors
                |> Array.map (fun (c, e) -> 
                    Chart.Line(e)
                        .WithStrokeColor(c)
                        .WithPointColor(c))
                |> Chart.Combine
                |> fun c -> Renderers.ChartJs.Render(c, Size = Size(600, 400), Config = cfg)
        }

    let legend =
        div (colors 
             |> Array.zip countries
             |> Array.map (fun (c, color) -> 
                div [
                    spanAttr [attr.style <| "width: 15px; height: 15px;
                                             margin-right: 10px;
                                             display: inline-block;
                                             background-color: " + color.ToString()] []
                    span [text c.Name]
                ] :> Doc))*)

    type Simple = JsonProvider<""" { "name":"John", "age":94 } """>
    type Numbers = JsonProvider<""" [1, 2, 3, 3.5] """>
    type Mixed = JsonProvider<""" [1, 2, "hello", "world"] """>
    type People = JsonProvider<""" [{ "name":"John", "age":94 }, { "name":"Tomas" }] """>
    type Values = JsonProvider<""" [{"value":94 }, {"value":"Tomas" }] """>
    type WorldBankJson = JsonProvider<"WorldBank.json">
    type GitHub = JsonProvider<"https://api.github.com/repos/fsharp/FSharp.Data/issues">

    let topRecentlyUpdatedIssues = 
        async {
            let! samples = GitHub.AsyncGetSamples()
            return 
                samples
                |> Seq.filter (fun issue -> issue.State = "open")
                |> Seq.sortBy (fun issue -> System.DateTime.Now - issue.UpdatedAt)
                |> Seq.truncate 5
        }

    let Main =
        (*let chrt =
            chart
            |> View.Const
            |> View.MapAsync id
            |> Doc.EmbedView

        Doc.Concat [
            h2 [text "Tertiary school enrollment (% gross)"]
            chrt
            legend
        ]
        |> Doc.RunById "main"*)

        let simple = Simple.Parse(""" { "name":"Tomas", "age":4 } """)
        Console.Log simple
        Console.Log simple.Age
        Console.Log simple.Name

        let nums = Numbers.Parse(""" [1, 45.28, 98.12, 5.345] """)
        Console.Log (Seq.sum nums)

        let mixed = Mixed.Parse(""" [4, 5, "hello", "world" ] """)

        mixed.Numbers |> Seq.sum |> Console.Log
        mixed.Strings |> String.concat ", " |> Console.Log

        for item in People.GetSamples() do 
            Console.Log (item.Name + " " )
            item.Age |> Option.iter (fun d -> Console.Log <| sprintf "(%d)" d)

        for item in Values.GetSamples() do 
            match item.Value.Number, item.Value.String with
            | Some num, _ -> Console.Log <| sprintf "Numeric: %d" num
            | _, Some str -> Console.Log <| sprintf "Text: %s" str
            | _ -> Console.Log <| sprintf "Some other value!"
        
        let docAsync = WorldBankJson.AsyncLoad("jsonp|http://api.worldbank.org/country/cz/indicator/GC.DOD.TOTL.GD.ZS?format=jsonp")
        async {
            let! a = docAsync
            for record in a.Array do
                record.Value |> Option.iter (fun v -> 
                    Console.Log <| sprintf "%d: %f" record.Date v)
        }
        |> Async.Start

        async {
            let! issues = topRecentlyUpdatedIssues
            for issue in issues do
                Console.Log <| sprintf "#%d %s" issue.Number issue.Title
        }
        |> Async.Start