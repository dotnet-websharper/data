// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2018 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}
namespace WebSharper.Data.Test

open WebSharper
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.Charting
open WebSharper.JavaScript

open FSharp.Data

[<JavaScript>]

module Client =    
    type WorldBank = WorldBankDataProvider<Asynchronous=true>
    let data = WorldBank.GetDataContext()

    let countries =
        [| data.Countries.Austria
           data.Countries.Hungary
           data.Countries.``United Kingdom``
           data.Countries.``United States`` |]

    let schoolEnrollment =
        countries
        |> Seq.map (fun c -> c.Indicators.``School enrollment, tertiary (gross), gender parity index (GPI)``)
        |> Async.Parallel

    let rand = System.Random()
    let randomColor () =
        let r = rand.Next 256
        let g = rand.Next 256
        let b = rand.Next 256
        Color.Rgba(r,g,b,1.)

    let colors = 
        countries |> Array.map (fun _ -> randomColor ())

    let chart =
        let cfg = 
            ChartJs.LineConfig()

        async {
            let! data = schoolEnrollment
            return
                data
                |> Array.map (fun i ->
                    Seq.zip (Seq.map string i.Years) i.Values
                )
                |> Array.zip colors
                |> Array.map (fun (c, e) -> 
                    Chart.Line(e)
                        .WithStrokeColor(c)
                        .WithPointColor(c))
                |> Chart.Combine
                |> fun c -> Renderers.ChartJs.Render(c, Size = Size(600, 400))//, Config = cfg)
        }

    let legend =
        div [] (colors 
            |> Array.zip countries
            |> Array.map (fun (c, color) -> 
                div [] [
                    span [attr.style <| "width: 15px; height: 15px;
                                        margin-right: 10px;
                                        display: inline-block;
                                        background-color: " + color.ToString()] []
                    span [] [text c.Name]
                ]))

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
                |> Array.ofSeq
                |> Array.filter (fun issue -> issue.State = "open")
                |> Array.sortBy (fun issue -> System.DateTime.Now - issue.UpdatedAt)
                |> Array.truncate 5
        }

    [<SPAEntryPoint>]
    let Main() =
        let chrt =
            chart
            |> View.ConstAsync
            |> Doc.EmbedView

        Doc.Concat [
            h2 [] [text "Tertiary school enrollment (% gross)"]
            chrt
            legend
        ]
        |> Doc.RunById "main"

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
            item.Age |> Option.iter (fun d -> printfn "(%d)" d)

        for item in Values.GetSamples() do 
            match item.Value.Number, item.Value.String with
            | Some num, _ -> printfn "Numeric: %d" num
            | _, Some str -> printfn "Text: %s" str
            | _ -> printfn "Some other value!"
        
        let docAsync = WorldBankJson.AsyncLoad("jsonp|http://api.worldbank.org/country/cz/indicator/GC.DOD.TOTL.GD.ZS?format=jsonp")
        async {
            let! a = docAsync
            Console.Log("doc", a)
            Console.Log("doc.Array", a.Array)
            for record in a.Array do
                record.Value |> Option.iter (fun v -> 
                    printfn "%d: %f" record.Date v)
        }
        |> Async.Start

        async {
            let! issues = topRecentlyUpdatedIssues
            printfn "Json: issues downloaded from GitHub:"
            for issue in issues do
                printfn "#%d %s" issue.Number issue.Title
        }
        |> Async.Start