﻿namespace PIIHide

open System.Reflection

// Examples
// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.tripledescryptoserviceprovider?view=netframework-4.8
// https://dotnetfiddle.net/8zMkWj

// take a converter type for to and from string and type of property
type PIIAttribute() = inherit System.Attribute()

module String =
    open System
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

module Typy =
    type M =
        | F of FieldInfo
        | P of PropertyInfo


module PII =
    let ENC_PREFIX = "ENC:"
    let hide key x =
        
        let enc (pi:PropertyInfo) (o:obj) =
            let value = pi.GetValue(o) |> string //use converter if type is not string
            let encValue = value |> Encryption.encrypt key
            let newValue = sprintf "%s%s" ENC_PREFIX encValue
            pi.SetValue(o,newValue)
            ignore()
            
        let t = x.GetType()
        let pis = t.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
                  |> Array.filter (fun pi -> pi.GetCustomAttributes<PIIAttribute>() |> Seq.isEmpty |> not )
        pis |> Array.iter (fun pi -> enc pi x)
        x
        
    let show key x =
        
        let dec (pi:PropertyInfo) (key:string) (o:obj) =
            let value = pi.GetValue(o) |> string //use converter if type is not string
            if(value |> String.startsWith ENC_PREFIX) then
                let noPrefix = value |> String.removePrefix ENC_PREFIX
                let decValue = noPrefix|> Encryption.decrypt key
                pi.SetValue(o,decValue)
                ignore()
            
        let t = x.GetType()
        let pis = t.GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
                  |> Array.filter (fun pi -> pi.GetCustomAttributes<PIIAttribute>() |> Seq.isEmpty |> not )
        pis |> Array.iter (fun pi -> dec pi key x)
        x
         
        
        
        