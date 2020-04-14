# PII Hide

This is an experimental library that aims to make it simple to encrypt/decrypt specific properties on an object.

> Currently just testing out. No Nuget yet. If you are interested let me know.

## Features

- Easy marking of personally identifiable information with the `PIIAttribute`
- Symmetric encryption of properties using AES
- Idempotent encryption and decryption
- Support for `string`, `DateTime`, and `DateTimeOffset` properties contained in a complex class
- Support for nested properties of complex type

## Usage

### C#

Although the library is written in F#, it does provide extension methods OR access via a static `Encryption` class that respects the C# naming conventions.

Mark the sensitive information with the `PIIAttribute`.

```csharp
using PIIHide;
//...
public class Person
{
    public long Id { get; set; }
    [PII]
    public string Name { get; set; }
    [PII]
    public Address Address { get; set; }
}

public class Address
{
    [PII]
    public string Street { get; set; }
    [PII]
    public string PostalCode { get; set; }
    public string Country { set; get; }
}
```

Then generate a key and call `Encrypt`/`Decrypt` on an instance of your class.

```csharp
using PIIHide;
using Encryption = PIIHide.CSharp.Encryption;
using PIIHide.CSharp.Extensions;
//...
var key = Encryption.MakeKey();
var person = MakePerson();
person.Encrypt(key);
person.Decrypt(key);
```

OR

```csharp
using PIIHide;
using Encryption = PIIHide.CSharp.Encryption;
//...
var key = Encryption.MakeKey();
var person = MakePerson();
Encryption.Encrypt(key, person);
Encryption.Decrypt(key, person);
```

See the [sample](/samples/csharp/ConsoleApp/) for what this would look like.

### F#

```fsharp
let person = aPerson()
let key = aKey()
PII.hide key person |> ignore
PII.show key person |> ignore
```