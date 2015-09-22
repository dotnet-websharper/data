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

    let Main =
        let chrt =
            async {
                let! school = data.Countries.Hungary.Indicators.``School enrollment, tertiary (% gross)``

                let data = Seq.zip (Seq.map string school.Years) school.Values
                return
                    Chart.Line(data)
                    |> fun c -> Renderers.ChartJs.Render(c, Window = 10)
            }
            |> View.Const
            |> View.MapAsync id
            |> Doc.EmbedView

        Doc.Concat [
            h3 [ text "Tertiary school enrollment in Hungary in the last 10 years, (% gross)" ]
            chrt
        ]
        |> Doc.RunById "main"
