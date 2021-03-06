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
    
[<Fact>]
let ``encrypt and decrypt a DateTimeOffset`` () =
    let value = DateTimeOffset.UtcNow
    let key = Guid.NewGuid().ToString().Substring(0,16)
    let enc = DateEncryption.encDtOff key value
    let result = DateEncryption.decDtOff key enc
    let y = enc.Year
    test <@ y > 3000 @>
    test <@ enc <> value @>
    test <@ result = value @>
    
[<Fact>]
let ``encrypt and decrypt a DateTime`` () =
    let value = DateTime.UtcNow
    let key = Guid.NewGuid().ToString().Substring(0,16)
    let enc = DateEncryption.encDt key value
    let result = DateEncryption.decDt key enc
    let y = enc.Year
    test <@ y > 3000 @>
    test <@ enc <> value @>
    test <@ result = value @>
    
[<Fact>]
let ``string encrypt is idempotent`` () =
    let value = "a test"
    let key = Guid.NewGuid().ToString().Substring(0,16)
    let enc1 = StringEncryption.encrypt key value
    let enc2 = StringEncryption.encrypt key value
    let result = StringEncryption.decrypt key enc2
    test <@ enc1 = enc2 @>
    test <@ result = value @>

[<Fact>]
let ``DateTime encrypt is idempotent`` () =
    let value = DateTime.UtcNow
    let key = Guid.NewGuid().ToString().Substring(0,16)
    let enc1 = DateEncryption.encDt key value
    let enc2 = DateEncryption.encDt key value
    let result = DateEncryption.decDt key enc2
    test <@ enc1 = enc2 @>
    test <@ result = value @>
    
[<Fact>]
let ``DateTimeOffset encrypt is idempotent`` () =
    let value = DateTimeOffset.UtcNow
    let key = Guid.NewGuid().ToString().Substring(0,16)
    let enc1 = DateEncryption.encDtOff key value
    let enc2 = DateEncryption.encDtOff key value
    let result = DateEncryption.decDtOff key enc2
    test <@ enc1 = enc2 @>
    test <@ result = value @>

