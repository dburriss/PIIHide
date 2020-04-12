module BoundaryTests

open System
open FsCheck
open FsCheck.Xunit
open PIIHide

let key = "3F009E95-E0D9-45"

type Encrypted = 
    static member DateTime() =
        Arb.Default.DateTime()
        |> Arb.mapFilter (DateEncryption.encDt key) (fun dt -> dt.Year > 3000)
    
    static member String() =       
        Arb.Default.NonEmptyString()
        |> Arb.mapFilter (fun (NonEmptyString s) -> s |> (StringEncryption.encrypt key) |> NonEmptyString) (fun _ -> true)

[<Properties( Arbitrary=[| typeof<Encrypted> |] )>]
module Decryption =
    
    [<Property>]
    let ``encrypted string start with ENC: and decrypted not``(NonEmptyString encrypted) =
        let decValue = StringEncryption.decrypt key encrypted
        encrypted |> String.startsWith "ENC:" && decValue |> String.startsWith "ENC:" |> not

    [<Property>]
    let ``decrypted dates are before 3000``(encrypted:DateTime) =
        let decDt = DateEncryption.decDt key encrypted
        encrypted.Year >= 3000 && decDt.Year < 3000
        
    [<Property>]
    let ``decrypted dates with offset are before 3000``(encrypted:DateTimeOffset) =
        let decDt = DateEncryption.decDtOff key encrypted
        encrypted.Year >= 3000 && decDt.Year < 3000