namespace WebSharper.Data

open WebSharper

[<Proxy(typeof<System.Func<_,_>>)>]
type SFunc<'A,'B>
    [<Inline "$f">](f : 'A -> 'B) =

    [<Inline "$0($par)">]
    member x.Invoke(par: 'A): 'B = failwith "client-side"

[<Proxy(typeof<System.IO.StringReader>)>]
type SReader
    [<Inline "$s">](s : string) = class end

//[<Proxy(typeof<Microsoft.FSharp.Core.Option<_>>)>]
//type FSharpOption<'T>
//    [<Inline "{$: 1, $0: $v}">](v: 'T) =
//    
//    [<Inline "{$: 1, $0: $v}">]
//    static member Some(v : 'T) : Option<'T> = failwith "client-side"
    