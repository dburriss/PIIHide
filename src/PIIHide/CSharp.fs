namespace PIIHide.CSharp

type PIIEncryption() =
    static member GenerateKey() = PIIHide.Crypto.makeKey()
    static member Encrypt (key, value) = PIIHide.PII.hide key value |> ignore
    static member Decrypt (key, value) = PIIHide.PII.show key value |> ignore
    
namespace PIIHide.CSharp.Extensions

open System.Runtime.CompilerServices
open PIIHide

[<Extension>]
type IEnumerableExtensions =
    [<Extension>] static member inline Encrypt(o: obj, key) = PII.hide key o |> ignore
    [<Extension>] static member inline Decrypt(o: obj, key) = PII.show key o |> ignore