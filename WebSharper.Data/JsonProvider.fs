namespace WebSharper.Data

open WebSharper
open WebSharper.JavaScript
open FSharp.Data

[<JavaScript>]
module private Utils = 
    [<Inline "$prop in $x" >]
    let HasProperty (x : obj) (prop : string) = X<bool>

    [<Inline>]
    let GetPropertyPacked (x : obj) (prop : string) =
        if HasProperty x prop then Some ((?) x prop)
        else None

[<JavaScript>]
module private JSRuntime = 
    let private matchTag tagCode (value : obj) : option<obj> = 
        if value ==. null then None
        elif JS.TypeOf(value) = JS.Kind.Boolean && tagCode = "Boolean" then Some(unbox value)
        elif JS.TypeOf(value) = JS.Kind.Number && tagCode = "Number" then Some(unbox value)
        elif JS.TypeOf(value) = JS.Kind.String && tagCode = "Number" then 
            let v = 1 * As value
            if JS.IsNaN v then None
            else Some (As v)
        elif JS.TypeOf(value) = JS.Kind.String && tagCode = "String" then Some(unbox value)
        elif Array.IsArray(value) && tagCode = "Array" then Some(unbox value)
        elif JS.TypeOf(value) = JS.Kind.Object && tagCode = "Record" then Some(unbox value)
        else None

    let GetArrayChildrenByTypeTag(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string, 
                                  mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T [] = 
        if Array.IsArray doc then 
            doc
            |> unbox
            |> Array.choose (matchTag tagCode)
            |> Array.map (unbox >> mapping.Invoke)
        else failwith "JSON mismatch: Expected Array node"

    let TryGetValueByTypeTag(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string, 
                             mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T option =
        matchTag tagCode doc
        |> Option.map (unbox >> mapping.Invoke)
    
    let TryGetArrayChildByTypeTag<'T>(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string, 
                                      mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T option = 
        let arr = GetArrayChildrenByTypeTag(doc, cultureStr, tagCode, mapping)
        if arr.Length = 1 then Some arr.[0]
        elif arr.Length = 0 then None
        else failwith "JSON mismatch: Expected Array with single or no elements."
    
    let GetArrayChildByTypeTag(value : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string) : Runtime.BaseTypes.IJsonDocument = 
#if ZAFIR
        let arr = GetArrayChildrenByTypeTag(value, cultureStr, tagCode, As <| fun x -> x)
#else
        let arr = GetArrayChildrenByTypeTag(value, cultureStr, tagCode, As <| FuncWithOnlyThis(fun x -> x))
#endif
        if arr.Length = 1 then arr.[0]
        else failwith "JSON mismatch: Expected single value, but found multiple."

[<JavaScript>]
module private TxtRuntime =
    let AsyncMap(comp : Async<'A>, mapping : System.Func<'A,'B>) : Async<'B> =
        async {
            let! c = comp
            return mapping.Invoke c
        }

[<Proxy(typeof<JsonValue>)>]
type private JsonValueProxy = 
    [<Inline; JavaScript>]
    static member Parse(text : string, ?cultureInfo : System.Globalization.CultureInfo) : JsonValue = 
        As <| JSON.Parse(text)

[<Proxy(typeof<FSharp.Data.Runtime.BaseTypes.JsonDocument>)>]
type private JsonDocument = 
    
    [<Inline "$0">]
    member x.get_JsonValue() = X<FSharp.Data.JsonValue>
    
    [<Inline; JavaScript>]
    static member Create(value : JsonValue, path : string) : Runtime.BaseTypes.IJsonDocument = As value

    [<Inline; JavaScript>]
    static member Create(reader:System.IO.TextReader, cultureStr:string): Runtime.BaseTypes.IJsonDocument =
        let data = As<obj> reader 
        if JS.TypeOf data = JS.Kind.String then 
            As <| JSON.Parse (As data)
        else As data

[<Proxy(typeof<FSharp.Data.Runtime.BaseTypes.IJsonDocument>)>]
type private IJsonDocument = 
    
    [<Inline "\"\"">]
    member x.Path() = X<string>

    [<Inline "$0">]
    member x.get_JsonValue() = X<JsonValue>

[<Proxy(typeof<FSharp.Data.Runtime.JsonValueOptionAndPath>)>]
type private JsonValueOptionAndPath = 
    
    [<Inline "\"\"">]
    member x.get_Path() = X<string>
        
    [<Inline "$0">]
    member x.get_JsonOpt() = X<JsonValue option>

[<Proxy(typeof<FSharp.Data.Runtime.TextRuntime>)>]
type private TextRuntime = 
    [<Inline; JavaScript>]
    static member AsyncMap (valueAsync:Async<'T>, mapping:System.Func<'T,'R>) : Async<'R> =
        TxtRuntime.AsyncMap(valueAsync, mapping)

    [<Inline"null">]
    static member GetCulture(cultureStr : string) = X<System.Globalization.CultureInfo>

#if ZAFIR
[<WebSharper.Proxy
#else
[<WebSharper.Core.Attributes.Proxy
#endif
    "FSharp.Data.Runtime.IO, \
     FSharp.Data, Culture=neutral, \
     PublicKeyToken=null">]
module private IO = 
    open WebSharper.JQuery
    
    [<JavaScript>]
    let asyncReadTextAtRuntime (forFSI: bool) (defaultResolutionFolder : string) (resolutionFolder : string)
                               (formatName : string) (encodingStr : string) (uri : string) : Async<System.IO.TextReader> =
        Async.FromContinuations <| fun (ok, ko, _) ->
            let (uri, jsonp) = 
                let l =uri.ToLower()
                if l.StartsWith("jsonp|")  then uri.Substring(6), true
                elif l.StartsWith("json|") then uri.Substring(5), false
                else uri, false
            let settings = 
                AjaxSettings(
                    DataType = DataType.Json,
#if ZAFIR
                    Success = (fun data _ _  -> ok <| As data),
                    Error = (fun _ _ err -> ko <| System.Exception(err)))
#else
                    Success = (fun (data,_,_) -> ok <| As data),
                    Error = (fun (_,_,err) -> ko <| System.Exception(err)))
#endif
            if jsonp then
                let fn = randomFunctionName ()
                settings.DataType <- DataType.Jsonp
                settings.Jsonp <- "prefix"
                settings.JsonpCallback <- "jsonp" + fn

            JQuery.Ajax(uri, settings) |> ignore

    [<JavaScript>]
    let asyncReadTextAtRuntimeWithDesignTimeRules (defaultResolutionFolder : string) 
                                                  (resolutionFolder : string) (formatName : string) 
                                                  (encodingStr : string) (uri : string) : Async<System.IO.TextReader> =
        asyncReadTextAtRuntime false defaultResolutionFolder resolutionFolder formatName encodingStr uri

[<Proxy(typeof<FSharp.Data.Runtime.JsonRuntime>)>]
type private JsonRuntime = 
    
    [<Inline"$doc[$name]">]
    static member GetPropertyPacked(doc : Runtime.BaseTypes.IJsonDocument, name : string) = X<Runtime.BaseTypes.IJsonDocument>
    
    [<Inline"$doc[$name]">]
    static member GetPropertyPackedOrNull(doc : Runtime.BaseTypes.IJsonDocument, name : string) = X<Runtime.BaseTypes.IJsonDocument>
    
    [<Inline; JavaScript>]
    static member TryGetPropertyPacked(doc : Runtime.BaseTypes.IJsonDocument, name : string) : Runtime.BaseTypes.IJsonDocument option = 
        Utils.GetPropertyPacked doc name
    
    [<Inline; JavaScript>]
    static member TryGetPropertyUnpackedWithPath(doc:Runtime.BaseTypes.IJsonDocument, name:string) : Runtime.JsonValueOptionAndPath =
        As <| Utils.GetPropertyPacked doc name

    [<Inline; JavaScript>]
    static member TryGetPropertyUnpacked (doc:Runtime.BaseTypes.IJsonDocument, name:string): JsonValue option =
        Utils.GetPropertyPacked doc name
    
    [<Inline; JavaScript>]
    static member ConvertString(cultureStr : string, json : JsonValue option) : string option = As json
    
    [<Inline; JavaScript>]
    static member ConvertBoolean(json : JsonValue option) : bool option = As json
    
    [<Inline; JavaScript>]
    static member ConvertFloat(cultureStr : string, missingValuesStr : string, json : JsonValue option) : float option = 
        Option.map (fun e -> 1. * As e) json
    
    [<Inline; JavaScript>]
    static member ConvertDecimal(cultureStr : string, json : JsonValue option) : decimal option =
        Option.map (fun e -> As (1. * As e)) json
    
    [<Inline; JavaScript>]
    static member ConvertInteger(cultureStr : string, json : JsonValue option) : int option =
        Option.map (fun e -> 1 * As e) json
    
    [<Inline; JavaScript>]
    static member ConvertInteger64(cultureStr : string, json : JsonValue option) : int64 option =
        Option.map (fun e -> 1L * As e) json
    
    [<Inline; JavaScript>]
    static member ConvertDateTime(cultureStr : string, json : JsonValue option) : System.DateTime option = As json
    
    [<Inline; JavaScript>]
    static member ConvertGuid(json : JsonValue option) : System.Guid option = As json
    
    [<Inline; JavaScript>]
    static member GetNonOptionalValue(path : string, opt : 'T option, originalValue : JsonValue option) : 'T = 
        match opt with
        | Some o -> o
        | None -> As null
    
    [<Inline; JavaScript>]
    static member GetArrayChildrenByTypeTag(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string, 
                                            mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T [] = 
        JSRuntime.GetArrayChildrenByTypeTag(doc, cultureStr, tagCode, mapping)
    
    [<Inline"$doc">]
    static member ConvertArray(doc : Runtime.BaseTypes.IJsonDocument, 
                               mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) = X<'T []>
    
    [<Inline; JavaScript>]
    static member ConvertOptionalProperty(doc : Runtime.BaseTypes.IJsonDocument, name : string, 
                                          mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T option = 
        if Utils.HasProperty doc name then Some(mapping.Invoke((?) doc name))
        else None
    
    [<Inline; JavaScript>]
    static member TryGetArrayChildByTypeTag(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string, 
                                            mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T option = 
        JSRuntime.TryGetArrayChildByTypeTag(doc, cultureStr, tagCode, mapping)
    
    [<Inline; JavaScript>]
    static member GetArrayChildByTypeTag(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string) : Runtime.BaseTypes.IJsonDocument = 
        JSRuntime.GetArrayChildByTypeTag(doc, cultureStr, tagCode)
    
    [<Inline; JavaScript>]
    static member TryGetValueByTypeTag(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string, 
                                       mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T option = 
        JSRuntime.TryGetValueByTypeTag(doc, cultureStr, tagCode, mapping)