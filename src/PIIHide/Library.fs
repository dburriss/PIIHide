﻿namespace PIIHide

open System

// Examples
// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.tripledescryptoserviceprovider?view=netframework-4.8
// https://dotnetfiddle.net/8zMkWj

// take a converter type for to and from string and type of property
type PIIAttribute() = inherit System.Attribute()

module String =
    open System
    open System.Text
    let startsWith (value:string) (s:string) = if(s |> isNull) then false else s.StartsWith(value)
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

module DateEncryption =
    // WARNING: Do not change these values once they have been used to encrypt
    let private ENC_IS_OVER = 3000 
    let private SHIFT_RANGE = 365000
    let private ENC_PREFIX = TimeSpan.FromDays(365000.0)
    let private dayShift key = (key.GetHashCode() % SHIFT_RANGE) |> Math.Abs |> float
    let private tsDiff shift = (ENC_PREFIX) + TimeSpan.FromDays(shift)
    
    let encDt (key:string) (dt:DateTime) =
        if(dt.Year > ENC_IS_OVER) then dt
        else
            let dayShift = key |> dayShift
            dt + tsDiff dayShift
    let encDtOff (key:string) (dt:DateTimeOffset) =
        if(dt.Year > ENC_IS_OVER) then dt
        else
            let dayShift = key |> dayShift
            dt + tsDiff dayShift
            
    let decDt (key:string) (dt:DateTime) =
        if(dt.Year > ENC_IS_OVER) then
            let dayShift = key |> dayShift
            dt - tsDiff dayShift
        else dt
    
    let decDtOff (key:string) (dt:DateTimeOffset) =
        if(dt.Year > ENC_IS_OVER) then
            let dayShift = key |> dayShift
            dt - tsDiff dayShift
        else dt

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

module StringEncryption =
    let private ENC_PREFIX = "ENC:"
    let encrypt key value =
        if(value |> String.startsWith ENC_PREFIX) then value
        else value |> Encryption.encrypt key |> sprintf "%s%s" ENC_PREFIX
        
    let decrypt key value =
        if(value |> String.startsWith ENC_PREFIX) then
            value |> String.removePrefix ENC_PREFIX |> Encryption.decrypt key
        else value
        
module PII =
    open System
    open System.Reflection
    open System.Collections.Generic

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
        
    let private encryptObj key (value:obj) =
        match value with
        | null -> null
        | :? string as s -> s |> StringEncryption.encrypt key |> box
        | :? DateTimeOffset as dt -> dt |> DateEncryption.encDtOff key |> box
        | :? DateTime as dt -> dt |> DateEncryption.encDt key |> box
        | _ -> failwithf "Encrypting %s type not supported." (value.GetType().FullName)
        
    let private enc (pi:PropertyInfo) (key:string, o:obj) =
        let value = pi.GetValue(o)
        let newValue = value |> encryptObj key
        pi.SetValue(o,newValue)
        (key, o)
    
    let private decryptObj key (value:obj) =
        match value with
        | :? string as s -> s |> StringEncryption.decrypt key |> box
        | :? DateTimeOffset as dt -> dt |> DateEncryption.decDtOff key |> box
        | :? DateTime as dt -> dt |> DateEncryption.decDt key |> box
        | _ -> failwithf "Decrypting %s type not supported." (value.GetType().FullName)
        
    let private dec (pi:PropertyInfo) (key:string, o:obj) =
        let value = pi.GetValue(o)
        let newValue = value |> decryptObj key
        pi.SetValue(o,newValue)
        (key, o)
        
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
    