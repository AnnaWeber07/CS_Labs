using Crypto.Rabbit;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace EncryptFile
{
    class EncryptFile
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Syntax: EncryptFile.exe <input-file> <output-file>");
                Console.WriteLine("  For decryption, swap input and output file");
                return;
            }

            byte[] key = new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x6, 0x7,
                0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0xE, 0xF
            };

            ICryptoTransform cipher = RabbitCipher.CreateEncryptor(key);

            byte[] buffer = new byte[10000];

            Stopwatch sw = new Stopwatch();
            sw.Start();

            using (FileStream infs = new FileStream(args[0], FileMode.Open, FileAccess.Read))
            using (FileStream outfs = new FileStream(args[1], FileMode.Create, FileAccess.Write))
            using (CryptoStream cs = new CryptoStream(outfs, cipher, CryptoStreamMode.Write))
            {
                while (true)
                {
                    int r = infs.Read(buffer, 0, buffer.Length);
                    if (r <= 0) break;
                    cs.Write(buffer, 0, r);
                }
            }

            sw.Stop();
            Console.WriteLine($"Elapsed time: {sw.ElapsedMilliseconds} ms");
        }
    }
}
