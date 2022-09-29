using System;

namespace CS_Labs
{
    class Program
    {
        public static void Main(string[] args)
        {

            Console.WriteLine("Write a string to encrypt:");
            string input = Console.ReadLine();
            Console.WriteLine();

            Console.WriteLine("Enter your int key:");
            int caesarKey = Int32.Parse(Console.ReadLine());
            Console.WriteLine();


            //Caesar
            Console.WriteLine("Caesar Cipher:");
            Console.WriteLine();

            Console.WriteLine("Encrypted data:");
            var caesarEncrypted = CaesarCipher.Encryption(input, caesarKey);
            Console.WriteLine(caesarEncrypted);

            Console.WriteLine("Decrypted data:");
            var caesarDecrypted = CaesarCipher.Decryption(caesarEncrypted, caesarKey);
            Console.WriteLine(caesarDecrypted);
            Console.WriteLine();


            //Playfair
            Console.WriteLine("Playfair Cipher:");
            Console.WriteLine();

            Console.WriteLine("Enter your string key:");
            string playfairKey = Console.ReadLine();
            Console.WriteLine();

            Console.WriteLine("Encrypted data:");
            var playfairEncrypted = PlayfairCipher.Encipher(input, playfairKey);
            Console.WriteLine(playfairEncrypted);

            Console.WriteLine("Decrypted data:");
            var playfairDecrypted = PlayfairCipher.Decipher(playfairEncrypted, playfairKey);
            Console.WriteLine(playfairDecrypted);
            Console.WriteLine();


            //Vignere
            Console.WriteLine("Vigenere Cipher:");
            Console.WriteLine();

            Console.WriteLine("Enter your string key:");
            string vigenereKey = Console.ReadLine();
            Console.WriteLine();

            Console.WriteLine("Encrypted data:");
            var vigenereEncrypted = VigenereCipher.Encipher(input, vigenereKey);
            Console.WriteLine(vigenereEncrypted);

            Console.WriteLine("Decrypted data:");
            var vigenereDecrypted = VigenereCipher.Decipher(vigenereEncrypted, vigenereKey);
            Console.WriteLine(vigenereDecrypted);
            Console.WriteLine();

            //Transposition
            Console.WriteLine("Transposition Cipher:");
            Console.WriteLine();

            Console.WriteLine("Enter your string key:");
            string transKey = Console.ReadLine();

            Console.WriteLine("Encrypted data:");
            var transEncrypted = TranspositionCipher.Encipher(input, transKey, '-');
            Console.WriteLine(transEncrypted);

            Console.WriteLine("Decrypted data:");
            var transDecrypted = TranspositionCipher.Decipher(transEncrypted, transKey).Trim('-');
            Console.WriteLine(transDecrypted);
            Console.WriteLine();

        }
    }
}
