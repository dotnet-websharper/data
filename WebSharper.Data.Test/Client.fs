namespace WebSharper.Data.Test

open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
open WebSharper.Charting

open FSharp.Data

[<JavaScript>]
module Client =    
    type WorldBank = WorldBankDataProvider<Asynchronous=true>
    let data = WorldBank.GetDataContext()

    let countries =
        [ data.Countries.Austria
          data.Countries.Hungary
          data.Countries.``United Kingdom``
          data.Countries.``United States`` ]

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
                |> Array.map (fun e -> Chart.Line(e).WithStrokeColor(randomColor ()))
                |> Chart.Combine
                |> fun c -> Renderers.ChartJs.Render(c, Size = Size(600, 400), Config = cfg)
        }

    let Main =
        let chrt =
            chart
            |> View.Const
            |> View.MapAsync id
            |> Doc.EmbedView

        Doc.Concat [
            chrt
        ]
        |> Doc.RunById "main"
