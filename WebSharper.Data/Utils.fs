namespace WebSharper.Data

open WebSharper

#if ZAFIR
#else
[<Proxy(typeof<System.Func<_,_>>)>]
type private SFunc<'A,'B>
    [<Inline "$f">](f : 'A -> 'B) =

    // The first parameter of a delegate is always translated to
    // "this" so we need to work around that here.
    [<Inline "(function(){return $0.call(arguments[0])})($value)">]
    member x.Invoke(value: 'A): 'B = failwith "client-side"
#endif

[<Proxy(typeof<System.IO.StringReader>)>]
type private SReader
    [<Inline "$s">](s : string) = class end

[<AutoOpen; JavaScript>]
module Pervasives =
    let randomFunctionName () =
        System.Guid.NewGuid().ToString().ToLower().Replace('-', '_')