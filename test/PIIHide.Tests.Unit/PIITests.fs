module PIITests

open System
open System.Diagnostics
open PIIHide
open Xunit
open Swensen.Unquote

type DisplayCustomer() =
    member val CustomerNumber = "" with get, set
    [<PII>] member val Name = "" with get, set
    
type CustomerActivity() =
    member val CustomerNumber = "" with get, set
    member val Orders = 0 with get, set
    [<PII>] member val CreatedAt = DateTime.MinValue with get, set
    [<PII>] member val LastActivity = DateTimeOffset.MinValue with get, set

type Customer() =
    member val CustomerNumber = "" with get, set
    [<PII>] member val Name = "" with get, set
    [<PII>] member val Address = Address() with get, set
    [<PII>] member val Birth = DateTime.MinValue with get, set
and Address() =
    [<PII>] member val Street = "" with get, set
    [<PII>] member val PostCode = "" with get, set
    [<PII>] member val Country = "" with get, set
    

let aDisplayCustomer() = DisplayCustomer(CustomerNumber="not pi", Name = "hide me")
let aCustomerActivity() = CustomerActivity(CustomerNumber="not pi", CreatedAt = DateTime.Now, LastActivity = DateTimeOffset.Now)
let aCustomer() =
    Customer(
                CustomerNumber="not pi",
                Name = "hide me",
                Address = Address(Street="1 Nowhere", PostCode="AZ0001", Country="Merica")
            )
let aKey() = Encryption.makeKey()

[<Fact>]
let ``hide encrypts all properties with PII attribute on simple string members`` () =
    let o = aDisplayCustomer()
    let key = aKey()
    let enc = o |> PII.hide key
    test <@ enc.CustomerNumber |> String.startsWith "ENC:" |> not @>
    test <@ enc.Name |> String.startsWith "ENC:" @>
    
[<Fact>]
let ``encrypting null returns null`` () =
    let o = aDisplayCustomer()
    o.Name <- null
    let key = aKey()
    let enc = o |> PII.hide key
    test <@ enc.Name = null @>
    
[<Fact>]
let ``hide encrypts all properties with PII attribute on simple date members`` () =
    let o = aCustomerActivity()
    let key = aKey()
    let enc = o |> PII.hide key
    test <@ enc.CreatedAt.Year > 3000 @>
    test <@ enc.LastActivity.Year > 3000 @>
    
[<Fact>]
let ``show decrypts all properties with PII attribute on simple string members`` () =
    let o = aDisplayCustomer()
    let key = aKey()
    PII.hide key o |> ignore
    test <@ o.Name |> String.startsWith "ENC:" @>
    PII.show key o |> ignore
    
[<Fact>]
let ``show decrypts all properties with PII attribute on simple date members`` () =
    let o1 = aCustomerActivity()
    let o2 = aCustomerActivity()
    o2.CreatedAt <- o1.CreatedAt
    o2.LastActivity <- o1.LastActivity
    let key = aKey()
    PII.hide key o1 |> ignore
    test <@ o1.CreatedAt.Year > 3000 @>
    test <@ o1.LastActivity.Year > 3000 @>
    PII.show key o1 |> ignore
    test <@ o1.CreatedAt = o2.CreatedAt @>
    test <@ o1.LastActivity = o2.LastActivity @>
    
[<Fact>]
let ``encrypt and decrypt 1 thousand simple objects in under 100ms`` () =
    let o = aCustomer()
    let key = aKey()
    let watch = Stopwatch.StartNew()
    
    for i = 1 to 1000 do
        PII.hide key o |> PII.show key |> ignore
        
    watch.Stop()
    test <@ watch.ElapsedMilliseconds < 100L @>
    
[<Fact>]
let ``encrypt an object encrypts PII members on nested member`` () =
    let o = aCustomer()
    let key = aKey()
    PII.hide key o |> ignore
    test <@ o.Address.Street |> String.startsWith "ENC:" @>
    test <@ o.Address.PostCode |> String.startsWith "ENC:" @>
    test <@ o.Address.Country |> String.startsWith "ENC:" @>
    
[<Fact>]
let ``decrypt an object encrypts PII members on nested member`` () =
    let o1 = aCustomer()
    let o2 = aCustomer()
    let key = aKey()
    PII.hide key o1 |> ignore
    PII.show key o1 |> ignore
    
    test <@ o1.Address.Street = o2.Address.Street @>
    test <@ o1.Address.PostCode = o2.Address.PostCode @>
    test <@ o1.Address.Country = o2.Address.Country @>