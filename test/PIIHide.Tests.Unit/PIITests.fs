module PIITests

open System.Diagnostics
open PIIHide
open Xunit
open Swensen.Unquote

type Purchase() =
    member val OrderNumber = "" with get, set
    [<PII>] member val CustomerName = "" with get, set
    [<PII>] member val Address = "" with get, set

let aSimpleClass() = Purchase(OrderNumber="not pi", CustomerName = "hide me")
let aKey() = Encryption.makeKey()

[<Fact>]
let ``hide encrypts all properties with PII attribute on simple string members`` () =
    let o = aSimpleClass()
    let key = aKey()
    let enc = o |> PII.hide key
    test <@ enc.OrderNumber |> String.startsWith "ENC:" |> not @>
    test <@ enc.CustomerName |> String.startsWith "ENC:" @>
    
[<Fact>]
let ``show decrypts all properties with PII attribute on simple string members`` () =
    let o = aSimpleClass()
    let key = aKey()
    PII.hide key o |> ignore
    test <@ o.CustomerName |> String.startsWith "ENC:" @>
    PII.show key o |> ignore
    test <@ o.CustomerName = (aSimpleClass()).CustomerName @>
    
[<Fact>]
let ``all string PII members are encrypted`` () =
    let o = aSimpleClass()
    let key = aKey()
    PII.hide key o |> ignore
    test <@ o.CustomerName |> String.startsWith "ENC:" @>
    test <@ o.Address |> String.startsWith "ENC:" @>
    
[<Fact>]
let ``encrypt and decrypt 1 thousand simple objects in under 50ms`` () =
    let o = aSimpleClass()
    let key = aKey()
    let watch = Stopwatch.StartNew()
    
    for i = 1 to 1000 do
        PII.hide key o |> PII.show key |> ignore
        
    watch.Stop()
    test <@ watch.ElapsedMilliseconds < 50L @>