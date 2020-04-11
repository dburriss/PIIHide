module Typy

open System
open System.Reflection

let assemblyFrom (t:Type) = t.Assembly
let assembliesFrom ts = ts |> Seq.map assemblyFrom
let typeInfos (assembly:Assembly) = assembly.DefinedTypes
let asType (ti:TypeInfo) = ti.AsType()
let isAssignable (abstraction:Type) (concretion:TypeInfo) = abstraction.IsAssignableFrom(concretion)
let isAssignableTo<'a> = isAssignable (typeof<'a>)
let typesEqual (t1:Type) (t2:Type) = t1.Name = t2.Name && t1.Namespace = t2.Namespace && t1.Module.Name = t2.Module.Name
let hasBaseType (abstraction:Type) (concretion:TypeInfo) =
    if(concretion.BaseType |> isNull) then
        false
    elif(concretion.BaseType = abstraction) then
        true
    else
        false

let rec isAncestorsOf (abstraction:Type) (concretion:TypeInfo) =
    if(concretion.BaseType |> isNull) then
        false
    elif (typesEqual concretion.BaseType abstraction) then
        true
    else
        isAncestorsOf abstraction (concretion.BaseType.GetTypeInfo())

let isAncestorsOf'<'a> = isAncestorsOf (typeof<'a>)
let isSimple (t:Type) =
    t.IsPrimitive
    || t = typeof<string>
    || t = typeof<DateTime>
    || t = typeof<DateTimeOffset>
let isAbstract (t:Type) = t.IsAbstract
let isNotAbstract t = not <| isAbstract t
let isGenTypeOf (gt:Type) (t:Type) =
    if(t.IsGenericType) then
        let tdef = t.GetGenericTypeDefinition()
        // A Hack because F# cannot get a generic def, only of T<Object>
        gt.Name = tdef.Name && gt.Namespace = tdef.Namespace
    else false
let fullname (t:Type) = t.FullName