using System;
using System.Text.Json;

using PIIHide;
using PIIHide.CSharp.Extensions;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var key = PII.GenerateKey();
            var person = MakePerson();
            PrintPerson(person, "A PERSON MODEL", ConsoleColor.Red);
            person.Encrypt(key);
            PrintPerson(person, "ENCRYPTED PERSON", ConsoleColor.Green);
            person.Decrypt(key);
            PrintPerson(person, "DECRYPTED PERSON", ConsoleColor.Red);
        }
        
        static Person MakePerson()
        {
            return new Person
            {
                Id = 1,
                Name = "Bob Builder",
                Address = new Address
                {
                    Street = "42 Quarry Lane",
                    PostalCode = "8U1LD3R",
                    Country = "England"
                }
            };
        }

        static void PrintPerson(Person person, string title, ConsoleColor color)
        {
            var jsonPerson = JsonSerializer.Serialize(person, JsonOptions);
            var curColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"===== {title} =====");
            Console.WriteLine(jsonPerson);
            Console.ForegroundColor = curColor;
        }
        
        private static JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

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
}
