namespace WebSharper.Data

open WebSharper
open WebSharper.JavaScript
open FSharp.Data

[<JavaScript>]
module private Utils = 
    let HasProperty (x : obj) (prop : string) = 
        if JS.TypeOf(x) <> JS.Kind.Undefined then true
        else false

[<JavaScript>]
module private JSRuntime = 
    let GetArrayChildrenByTypeTag(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string, 
                                  mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T [] = 
        let matchTag (value : obj) : option<obj> = 
            if value ==. null then None
            elif JS.TypeOf(value) = JS.Kind.Boolean && tagCode = "Boolean" then Some(unbox value)
            elif JS.TypeOf(value) = JS.Kind.Number && tagCode = "Number" then Some(unbox value)
            elif JS.TypeOf(value) = JS.Kind.String && tagCode = "Number" then Some(unbox (1 * unbox value))
            elif JS.TypeOf(value) = JS.Kind.String && tagCode = "String" then Some(unbox value)
            elif Array.IsArray(value) && tagCode = "Array" then Some(unbox value)
            elif JS.TypeOf(value) = JS.Kind.Object && tagCode = "Record" then Some(unbox value)
            else None // ??? maybe sometimes fail
        if Array.IsArray doc then 
            doc
            |> unbox
            |> Array.choose matchTag
            |> Array.map (unbox >> mapping.Invoke)
        else failwith "JSON mismatch: Expected Array node"
    
    let TryGetArrayChildByTypeTag<'T>(doc : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string, 
                                      mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T option = 
        let arr = GetArrayChildrenByTypeTag(doc, cultureStr, tagCode, mapping)
        if arr.Length = 1 then Some arr.[0]
        elif arr.Length = 0 then None
        else failwith "JSON mismatch: Expected Array with single or no elements."
    
    let GetArrayChildByTypeTag(value : Runtime.BaseTypes.IJsonDocument, cultureStr : string, tagCode : string) : Runtime.BaseTypes.IJsonDocument = 
        let arr = GetArrayChildrenByTypeTag(value, cultureStr, tagCode, System.Func<_, _>(id))
        if arr.Length = 1 then arr.[0]
        else failwith "JSON mismatch: Expected single value, but found multiple."

[<Proxy(typeof<JsonValue>)>]
type private JsonValueProxy = 
    [<Inline; JavaScript>]
    static member Parse(text : string, ?cultureInfo : System.Globalization.CultureInfo) : JsonValue = 
        As <| JSON.Parse(text)

[<Proxy(typeof<FSharp.Data.Runtime.BaseTypes.JsonDocument>)>]
type private JsonDocument = 
    
    [<Inline; JavaScript>]
    member x.JsonValue : JsonValue = As x
    
    [<Inline; JavaScript>]
    static member Create(value : JsonValue, path : string) : Runtime.BaseTypes.IJsonDocument = As value

    [<Inline; JavaScript>]
    static member Create(reader:System.IO.TextReader, cultureStr:string): Runtime.BaseTypes.IJsonDocument = 
        As (JSON.Parse <| As reader)

[<Proxy(typeof<FSharp.Data.Runtime.BaseTypes.IJsonDocument>)>]
type private IJsonDocument = 
    
    [<Inline "\"\"">]
    member x.Path() : string = failwith "client-side"

    [<Inline "$0">]
    member x.get_JsonValue() : JsonValue = failwith "client-side"

[<Proxy(typeof<FSharp.Data.Runtime.JsonValueOptionAndPath>)>]
type private JsonValueOptionAndPath = 
    
    [<Inline "\"\"">]
    member x.get_Path() : string = failwith "client-side"
        
    [<Inline "$0">]
    member x.get_JsonOpt() : JsonValue option = failwith "client-side"

[<Proxy(typeof<FSharp.Data.Runtime.TextRuntime>)>]
type private TextRuntime = 
    [<Inline"null">]
    static member GetCulture(cultureStr : string) : System.Globalization.CultureInfo = failwith "client-side"

[<Proxy(typeof<FSharp.Data.Runtime.JsonRuntime>)>]
type private JsonRuntime = 
    
    [<Inline"$doc[$name]">]
    static member GetPropertyPacked(doc : Runtime.BaseTypes.IJsonDocument, name : string) : Runtime.BaseTypes.IJsonDocument = 
        failwith "client-side"
    
    [<Inline"$doc[$name]">]
    static member GetPropertyPackedOrNull(doc : Runtime.BaseTypes.IJsonDocument, name : string) : Runtime.BaseTypes.IJsonDocument = 
        failwith "client-side"
    
    [<Inline; JavaScript>]
    static member TryGetPropertyPacked(doc : Runtime.BaseTypes.IJsonDocument, name : string) : Runtime.BaseTypes.IJsonDocument option = 
        if JS.TypeOf(doc) <> JS.Kind.Undefined then Some <| As((?) doc name)
        else None
    
    [<Inline; JavaScript>]
    static member TryGetPropertyUnpackedWithPath(doc:Runtime.BaseTypes.IJsonDocument, name:string) : Runtime.JsonValueOptionAndPath =
        if JS.TypeOf(doc) <> JS.Kind.Undefined then As <| Some((?) doc name)
        else As None
    
    [<Inline; JavaScript>]
    static member ConvertString(cultureStr : string, json : JsonValue option) : string option = As json
    
    [<Inline; JavaScript>]
    static member ConvertBoolean(json : JsonValue option) : bool option = As json
    
    [<Inline; JavaScript>]
    static member ConvertFloat(cultureStr : string, missingValuesStr : string, json : JsonValue option) : float option = 
        As json
    
    [<Inline; JavaScript>]
    static member ConvertDecimal(cultureStr : string, json : JsonValue option) : decimal option = As json
    
    [<Inline; JavaScript>]
    static member ConvertInteger(cultureStr : string, json : JsonValue option) : int option = As json
    
    [<Inline; JavaScript>]
    static member ConvertInteger64(cultureStr : string, json : JsonValue option) : int64 option = As json
    
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
                               mapping : System.Func<Runtime.BaseTypes.IJsonDocument, 'T>) : 'T [] = 
        failwith "client-side"
    
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
        JSRuntime.TryGetArrayChildByTypeTag(doc, cultureStr, tagCode, mapping)
