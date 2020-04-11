module PIITests

open System
open System.Diagnostics
open PIIHide
open Xunit
open Swensen.Unquote

type Customer() =
    member val CustomerNumber = Guid.NewGuid() with get, set
    member val Reference = "" with get, set
    [<PII>] member val Name = "" with get, set
    [<PII>] member val Address = "" with get, set
    //[<PII>] member val Birth = DateTimeOffset.MinValue with get, set

let aSimpleClass() = Customer(Name = "hide me")
let aKey() = Encryption.makeKey()

[<Fact>]
let ``hide encrypts all properties with PII attribute on simple string members`` () =
    let o = aSimpleClass()
    let key = aKey()
    let enc = o |> PII.hide key
    test <@ enc.Reference |> String.startsWith "ENC:" |> not @>
    test <@ enc.Name |> String.startsWith "ENC:" @>
    
[<Fact>]
let ``show decrypts all properties with PII attribute on simple string members`` () =
    let o = aSimpleClass()
    let key = aKey()
    PII.hide key o |> ignore
    test <@ o.Name |> String.startsWith "ENC:" @>
    PII.show key o |> ignore
    test <@ o.Name = (aSimpleClass()).Name @>
    
[<Fact>]
let ``all string PII members are encrypted`` () =
    let o = aSimpleClass()
    let key = aKey()
    PII.hide key o |> ignore
    test <@ o.Name |> String.startsWith "ENC:" @>
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
    
[<Fact>]
let ``DefaultConverter can convert strings`` () =
    let converter = DefaultConverter() :> IStringConverter
    let s = "test"
    let o = "test" :> obj
    test <@ converter.CanConvert(typeof<string>) = true @>
    test <@ converter.FromAString(s) = o @>
    test <@ converter.ToAString(o) = s @>