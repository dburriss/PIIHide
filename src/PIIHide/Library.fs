namespace PIIHide

open System
open System.Reflection
// Examples
// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.tripledescryptoserviceprovider?view=netframework-4.8
// https://dotnetfiddle.net/8zMkWj

// take a converter type for to and from string and type of property
type IStringConverter =
    abstract member CanConvert : t:Type -> bool
    abstract member ToAString : value:obj -> string
    abstract member FromAString : value:string -> obj
    
type DefaultConverter() =
    interface IStringConverter with
        member this.CanConvert t =
            match t with
            | t when t = typeof<string> -> true
            | _ -> false
        member this.ToAString (value:obj) = value.ToString()
        member this.FromAString (value:string) = value :> obj

type PIIAttribute(converterType:TypeInfo) =
    inherit System.Attribute()
    do
        if(converterType |> (Typy.isAssignableTo<IStringConverter>) |> not) then
            failwithf "%s does not implement IStringConverter." converterType.FullName
            
    let converter = Activator.CreateInstance(converterType |> Typy.asType)  
    new() = PIIAttribute(converterType = typeof<DefaultConverter>.GetTypeInfo())
    member val Converter = converter

module String =
    open System.Text
    let startsWith (value:string) (s:string) = s.StartsWith(value)
    let sub start len (s:string) = s.Substring(start, len)
    let utf8Bytes (s:string) = UTF8Encoding.UTF8.GetBytes(s)
    let utf8String arr = UTF8Encoding.UTF8.GetString(arr)
    let stringToBytes = utf8Bytes
    let fromBase64 (s:string) = Convert.FromBase64String(s)
    let toBase64 arr = Convert.ToBase64String(arr, 0, arr.Length)
    let removePrefix (prefix:string) (s:string) =
            let start = prefix.Length
            let len = ((s.Length) - start)
            s |> sub start len

//provide defaults for int, DateTime
module Encryption =
    
    open System
    open System.Security.Cryptography
    
    // HELPERS
    let private mode = CipherMode.ECB
    let private padding = PaddingMode.PKCS7
    
    let private createProvider toBytes (key : string)=
        let tripleDES = new AesCryptoServiceProvider()
        tripleDES.Key <- toBytes key
        tripleDES.Mode <- mode
        tripleDES.Padding <- padding
        tripleDES
        
    let private encryptionTransform toBytes (provider:#SymmetricAlgorithm) value =
        let inputArray = toBytes value
        use cTransform = provider.CreateEncryptor()
        cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length)
        
    let private decryptionTransform (provider:#SymmetricAlgorithm) value =
        let inputArray = String.fromBase64 value
        use cTransform = provider.CreateDecryptor()
        cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length)
        
    // IMPLEMENTATION
    let makeKey() = Guid.NewGuid().ToString().Substring(0,16)
    let encrypt (key:string) (value:string) =
        use provider = createProvider String.stringToBytes key
        let resultArr = encryptionTransform String.stringToBytes provider value
        provider.Clear()
        String.toBase64 resultArr
        
    let decrypt (key:string) (input:string) =
        use provider = createProvider String.stringToBytes key
        let resultArr = decryptionTransform provider input
        provider.Clear()
        String.utf8String resultArr 


module PII =
    open System
    open System.Reflection
    open System.Collections.Generic
    
    let ENC_PREFIX = "ENC:"
    
    // HELPERS
    let private ffold fs = fs |> (Seq.fold (fun f s -> f >> s) id)
    let private propsWithPii (t:Type) =
        t.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
        |> Array.filter (fun pi -> pi.GetCustomAttributes<PIIAttribute>() |> Seq.isEmpty |> not )
        
    let private makeMemberUpdateF f pi  = f pi
    let private makeUpdateF f props =
        props
        |> Seq.map (makeMemberUpdateF f)
        |> ffold
        
    let updateCache = Dictionary<Type,((string * obj -> string * obj) * (string * obj -> string * obj))>()
    let private memoization (cache:Dictionary<_, _>) (f: 'a -> 'b) =
        (fun x ->
            match cache.TryGetValue(x) with
            | true, cachedValue -> cachedValue
            | _ -> 
                let result = f x
                cache.Add(x, result)
                result)
    let private transformers encF decF (t:Type) =
        
        let props = t |> propsWithPii
        let encUpdate = makeUpdateF encF props
        let decUpdate = makeUpdateF decF props
        (encUpdate,decUpdate)
        
    let private enc (pi:PropertyInfo) (key:string, o:obj) =
        let value = pi.GetValue(o) |> string //use converter if type is not string
        let encValue = value |> Encryption.encrypt key
        let newValue = sprintf "%s%s" ENC_PREFIX encValue
        pi.SetValue(o,newValue)
        (key, o)
        
    let private dec (pi:PropertyInfo) (key:string, o:obj) =
        let value = pi.GetValue(o) |> string //use converter if type is not string
        if(value |> String.startsWith ENC_PREFIX) then
            let noPrefix = value |> String.removePrefix ENC_PREFIX
            let decValue = noPrefix |> Encryption.decrypt key
            pi.SetValue(o,decValue)
            (key, o)
        else (key, o)
        
    let private encryptionTransformers (t:Type) = t |> memoization updateCache (transformers enc dec)
    
    // IMPLEMENTATION
    let hide key x =
        let (encryptObj,_)  = x.GetType() |> encryptionTransformers
        encryptObj (key, x) |> ignore
        x
        
    let show key x =
        let (_,decryptObj)  = x.GetType() |> encryptionTransformers
        decryptObj (key, x) |> ignore
        x
    