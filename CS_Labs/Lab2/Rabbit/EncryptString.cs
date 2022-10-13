using Crypto.Rabbit;
using System;
using System.Text;

namespace EncryptString
{
    class EncryptString
    {
        static void Main(string[] args)
        {
            byte[] key = new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x6, 0x7,
                0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0xE, 0xF
            };

            string plainText = "This text will be encripted";
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            // Encoding 

            RabbitCipher rabbit = new RabbitCipher(key);
            byte[] encoded = new byte[plainBytes.Length];

            for (int j = 0; j < plainBytes.Length; j++)
            {
                encoded[j] = rabbit.CipherByte(plainBytes[j]);
            }

            // Decoding

            RabbitCipher rabbit2 = new RabbitCipher(key);
            byte[] decoded = new byte[encoded.Length];

            for (int j = 0; j < encoded.Length; j++)
            {
                decoded[j] = rabbit2.CipherByte(encoded[j]);
            }

            Console.WriteLine($"Plain  : {plainText}");
            Console.WriteLine($"Encoded: {Encoding.UTF8.GetString(encoded)}");
            Console.WriteLine($"Decoded: {Encoding.UTF8.GetString(decoded)}");
        }
    }
}
