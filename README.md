# WebSharper.Data

WebSharper.Data provides proxies for the [FSharp.Data][1]
project to enable you to write data-rich applications on the client side.

## Supported Providers

Currently WebSharper.Data implements the `JsonProvider` and the `WorldBankProvider`
runtimes meaning these are the ones you can acces from the client-side. After installing
the package you should be able to use these types from [FSharp.Data][1], but using other types
will result in the WebSharper Compiler's being unable to compile the code.

## Additional Notes

Since the [World Bank API][2] is only available through [JSONP][4] from JavaScript due to
cross-origin issues the implementation of the runtime in JavaScript uses [JSONP][4] to
request data from the API as opposed to [FSharp.Data][1] which uses the JSON API.
This has no effect on anything from the user's point of view.

Since a lot of providers don't give access to their JSON APIs for JavaScript clients
because of cross-origin issues (like mentioned above in the case of the [World Bank Database][3]), we sometimes need to use [JSONP][4] instead. To get around this you can
specify your URL in WebSharper.Data like this:

```fsharp
open FSharp.Data

type WorldBank = JsonProvider<"WorldBank.json">
let docAsync = WorldBankJson.AsyncLoad("jsonp|http://api.worldbank.org/country/cz/indicator/GC.DOD.TOTL.GD.ZS?format=[JSONP][4]")
  async {
      let! a = docAsync
      for record in a.Array do
          record.Value |> Option.iter (fun v ->
              printfn "%d: %f" record.Date v)
  }
  |> Async.Start
```
here `"jsonp|http://url.com"` means that you want to use [JSONP][4] instead JSON.

Due to the nature of JavaScript, `Async` and the implementation of `JsonProviderRuntime` you
can only use the async versions of the fetching functions. It would be technically possible
to request data synchronously with Ajax but it would be undesirable and it's currently impossible to implement in WebSharper because the `JsonProvider` uses `Async.RunSynchronously` which cannot be translated to JavaScript.

Special thanks to the [FunScript][5] folks for their implementation of the runtimes and showing it's possible (and easy) to do once you have it figured out. Some code and a lot of ideas have been reused from their work.


[1]: http://fsharp.github.io/FSharp.Data/
[2]: http://data.worldbank.org/developers/api-overview
[3]: http://data.worldbank.org/
[4]: http://json-p.org/
[5]: https://github.com/ZachBray/FunScript
