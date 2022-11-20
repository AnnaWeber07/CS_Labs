using RsaAlgorithm;
using System;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;

namespace DH
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //enter p and q values:

            Console.WriteLine("Please enter a string:");
            string input = Console.ReadLine();

            Console.WriteLine("Please enter two big prime numbers p and q");
            Console.Write("P:");
            long p = Convert.ToInt64(Console.ReadLine());
            Console.Write("Q:");
            long q = Convert.ToInt64(Console.ReadLine());

            Encryption encryption = new Encryption(p, q, input);
            Decryption decryption = new Decryption(encryption);


            Console.WriteLine("This is your encrypted message: ");
            encryption.EncryptionProcess();


            Console.WriteLine("This is your decrypted message: ");
            decryption.DecryptionProcess();




        }
    }
}
