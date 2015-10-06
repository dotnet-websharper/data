namespace WebSharper.Data

open WebSharper
open FSharp.Data

[<Proxy(typeof<Runtime.WorldBank.WorldBankData>)>]
type private WorldBankData 
    [<Inline "{serviceUrl: $serviceUrl, source: $sources}">] 
    (serviceUrl : string, sources : string) = 

    member x.ServiceUrl
        with [<Inline "$0.serviceUrl">] get() = failwith "Client-side"

    member x.Sources
        with [<Inline "$0.sources">] get() = failwith "Client-side"

[<JavaScript>]
module private WBRuntime =
    open WebSharper.JQuery
    open WebSharper.JavaScript

    type WorldBankCountries = WorldBankData

    type WorldBankCountry =
        { Context : WorldBankData
          Code : string 
          Name : string }

    type WorldBankIndicators = WorldBankCountry
          
    let worldBankUrl (wb:WorldBankData) (functions: string list) (props: (string * string) list) = 
        wb.ServiceUrl + "/" +
        (functions |> List.map (fun m -> "/" + JS.EncodeURIComponent(m)) |> String.concat "") +
        "?per_page=1000" +
        (props |> List.map (fun (key, value) -> "&" + key + "=" + JS.EncodeURIComponent(value:string)) |> String.concat "")

    type WorldBankRuntime =

        static member GetCountry(countries:WorldBankCountries, code:string, name:string) : WorldBankCountry =
            { Context = countries
              Code = code 
              Name = name }

        static member GetIndicators(country:WorldBankCountry) : WorldBankIndicators =
            country

        static member AsyncGetIndicator(country:WorldBankIndicators, indicator:string) : Async<obj> =
            Async.FromContinuations(fun (ok, ko, _) ->
                let guid = randomFunctionName ()
                let wb = country.Context
                let countryCode = country.Code
                let url = worldBankUrl wb [ "countries"; countryCode; "indicators"; indicator ] [ "date", "1900:2050"; "format", "jsonp" ]
                JQuery.Ajax(
                    AjaxSettings(
                        Url = url,
                        DataType = DataType.Jsonp,
                        Jsonp = "prefix",
                        JsonpCallback = "jsonp" + guid,
                        Error = (fun (jqXHR, textStatus, error) -> 
                            ko <| System.Exception(textStatus + error)),
                        Success = (fun (data, textStatus, jqXHR) ->
                            let data = As<obj []> data
                            let res =
                                (data.[1] :?> obj []) 
                                |> Array.choose (fun e ->
                                    if e?value ==. null then None
                                    else Some(e?date, e?value))
                                |> Array.rev

                            ok <| New res)
                    )
                ) |> ignore
            ) 

open System.Collections.Generic
open WebSharper.JavaScript
open WBRuntime

[<Proxy(typeof<Runtime.WorldBank.IWorldBankData>)>]
type private IWorldBankData =
    
    [<Inline "$this">]
    member x.GetCountries<'T>() = X<seq<'T>>

[<Proxy(typeof<Runtime.WorldBank.ICountry>)>]
type private ICountry =
    
    [<JavaScript; Inline>]
    member x.GetIndicators() : Runtime.WorldBank.Indicators =
        As <| WorldBankRuntime.GetIndicators(As x)
    
[<Proxy(typeof<Runtime.WorldBank.ICountryCollection>)>]
type private ICountryCollection =
    
    [<JavaScript; Inline>]
    member x.GetCountry(code, name) : Runtime.WorldBank.Country =
        As <| WorldBankRuntime.GetCountry(As x, code, name)

[<Proxy(typeof<Runtime.WorldBank.IIndicators>)>]
type private IIndicators = 
    
    [<JavaScript; Inline>]
    member x.AsyncGetIndicator(indicator) : Async<Runtime.WorldBank.Indicator> =
        As <| WorldBankRuntime.AsyncGetIndicator(As x, indicator)

[<Proxy(typeof<Runtime.WorldBank.Country>)>]
type private Country =
    
    member x.Name
        with [<Inline "$this.Name">] get () = X<string>

    member x.Code
        with [<Inline "$this.Code">] get () = X<string>

[<Proxy(typeof<Runtime.WorldBank.Indicator>)>]
type private Indicator =
    
    member x.Item 
        with [<JavaScript; Inline>] get (year : int) =
            let e = (?) x (string year)
            if e ==. JS.Undefined then JS.NaN
            else As e

    member x.TryGetValueAt (year : int) =
        let e = (?) x (string year)
        if e ==. JS.Undefined then None
        else Some <| As e

    member x.Years 
        with [<JavaScript; Inline>] get () =
            JS.GetFieldNames(x)
            |> Seq.map int
            |> Seq.sort
            |> fun e -> new List<_>(e) :> ICollection<_>

    member x.Values
        with [<JavaScript; Inline>] get () =
            JS.GetFields(x)
            |> Seq.sortBy (fun (a, _) -> int a)
            |> Seq.map (snd >> As<float>)
            |> fun e -> new List<_>(e) :> ICollection<_>
        