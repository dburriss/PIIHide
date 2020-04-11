module PIITests

open PIIHide
open Xunit
open Swensen.Unquote

type SimpleStringClass() =
    member val NotPii = "" with get, set
    [<PII>] member val StringPii = "" with get, set

let aSimpleClass() = SimpleStringClass(NotPii="not pi", StringPii = "hide me")
let aKey() = Encryption.makeKey()

[<Fact>]
let ``hide encrypts all properties with PII attribute on simple string members`` () =
    let o = aSimpleClass()
    let key = aKey()
    let enc = o |> PII.hide key
    test <@ enc.NotPii |> String.startsWith "ENC:" |> not @>
    test <@ enc.StringPii |> String.startsWith "ENC:" @>
    
[<Fact>]
let ``show decrypts all properties with PII attribute on simple string members`` () =
    let o = aSimpleClass()
    let key = aKey()
    PII.hide key o |> ignore
    test <@ o.StringPii |> String.startsWith "ENC:" @>
    PII.show key o |> ignore
    test <@ o.StringPii = (aSimpleClass()).StringPii @>