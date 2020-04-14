# Sample

This sample C# console application creates a `Person` prints it, encrypts and prints, and then decrypts and prints it again.  
Below you can see the output.

```bash
===== A PERSON MODEL =====
{
  "Id": 1,
  "Name": "Bob Builder",
  "Address": {
    "Street": "42 Quarry Lane",
    "PostalCode": "8U1LD3R",
    "Country": "England"
  }
}
===== ENCRYPTED PERSON =====
{
  "Id": 1,
  "Name": "ENC:1HR8SDOSAjrzLwuua1B\u002BFQ==",
  "Address": {
    "Street": "ENC:sBLLAYYGPdmB5VtuO6nl7w==",
    "PostalCode": "ENC:GeOdayNirq3utlWoAeV7sw==",
    "Country": "England"
  }
}
===== DECRYPTED PERSON =====
{
  "Id": 1,
  "Name": "Bob Builder",
  "Address": {
    "Street": "42 Quarry Lane",
    "PostalCode": "8U1LD3R",
    "Country": "England"
  }
}

```