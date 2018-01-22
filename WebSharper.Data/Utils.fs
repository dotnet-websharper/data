namespace WebSharper.Data

open WebSharper

[<Proxy(typeof<System.IO.StringReader>)>]
type private SReader
    [<Inline "$s">](s : string) = class end

[<AutoOpen; JavaScript>]
module Pervasives =
    let randomFunctionName () =
        System.Guid.NewGuid().ToString().ToLower().Replace('-', '_')
