module Typy

open System
open System.Reflection

let private stringType = typeof<string>
let private dtType = typeof<DateTime>
let private dtOffType = typeof<DateTimeOffset>
let isSimple (t:Type) =
    t.IsPrimitive
    || t = stringType
    || t = dtType
    || t = dtOffType
    
let private bclAssembly = typeof<string>.Assembly.FullName
let isBcl (t:Type) = t.Assembly.FullName = bclAssembly

let isCustom (t:Type) = t |> isSimple |> not && t |> isBcl |> not

let propInfoWithAttr<'a when 'a :> Attribute> bindingFlags (t:Type) =
        t.GetProperties(bindingFlags)
        |> Array.filter (fun pi -> pi.GetCustomAttributes<'a>() |> Seq.isEmpty |> not )