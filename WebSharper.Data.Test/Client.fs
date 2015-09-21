namespace WebSharper.Data.Test

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Client
open WebSharper.Charting

open FSharp.Data

[<JavaScript>]
module Client =    
    type WorldBank = WorldBankDataProvider<Asynchronous=true>
    let data = WorldBank.GetDataContext()

    let Main =

        let d = div []

        let chrt =
            async {
                let! school = data.Countries.Hungary.Indicators.``School enrollment, tertiary (% gross)``

                let data = Seq.zip (Seq.map string school.Years) school.Values
                let chrt =
                    Chart.Line(data)
                    |> fun c -> Renderers.ChartJs.Render(c, Window = 10)
                d.Append <| Doc.Static chrt.Dom
                chrt.Render()
            }
            |> View.Const
            |> View.MapAsync id
            |> View.Map (fun () -> Doc.Empty)
            |> Doc.EmbedView

        Doc.Concat [
            h3 [ text "Tertiary school enrollment in Hungary in the last 10 years, (% gross)" ]
            d
            chrt
        ]
        |> Doc.RunById "main"
