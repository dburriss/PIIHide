module EncryptionTests

open System
open PIIHide
open Xunit
open Swensen.Unquote

[<Fact>]
let ``encrypt returns key and value`` () =
    let enc = Encryption.encrypt "sblw-3hn8-sqoy19" "bob"
    test <@ enc |> isNull |> not @>
    
[<Fact>]
let ``encrypt decrypt are symmetric`` () =
    let value = "bob"
    let key = "sblw-3hn8-sqoy19"
    let enc = Encryption.encrypt key value
    let result = Encryption.decrypt key enc
    test <@ result = value @>
    
[<Fact>]
let ``generated key accepted`` () =
    let value = "bob"
    let key = Guid.NewGuid().ToString().Substring(0,16)
    let enc = Encryption.encrypt key value
    let result = Encryption.decrypt key enc
    test <@ result = value @>

