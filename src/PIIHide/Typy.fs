module Typy

open System

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