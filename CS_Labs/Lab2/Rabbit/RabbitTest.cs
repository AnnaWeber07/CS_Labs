using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Crypto.Rabbit.Test
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc4503
    /// Appendix A
    /// </summary>

    public class RabbitTest
    {
        static byte[] StringToByteArray(string hexStr)
        {
            return hexStr.Split(' ')
                .Select(c => Convert.ToByte(c, 16))
                .ToArray();
        }

        static string BytesToString(byte[] b)
        {
            StringBuilder sb = new StringBuilder();
            string comma = string.Empty;
            foreach (var v in b)
            {
                sb.Append(comma);
                sb.Append(v.ToString("X2"));
                comma = "-";
            }
            return sb.ToString();
        }

        static void AssertEqual(byte[] b1, byte[] b2)
        {
            Console.WriteLine("State   : " + BytesToString(b2));
            Console.WriteLine("Expected: " + BytesToString(b1));

            if (b1.Length != b2.Length)
                throw new Exception("Parameters must have same length");

            for (int i = 0; i < b1.Length; i++)
                if (b1[i] != b2[i])
                    throw new Exception($"Different parameters at index {i}");
        }

        public static void PerformValidationTests()
        {
            RabbitTest rabbitTest = new RabbitTest();
            rabbitTest.Test_A1_1();
            rabbitTest.Test_A1_2();
            rabbitTest.Test_A1_3();
            rabbitTest.Test_A2_1();
            rabbitTest.Test_A2_2();
            rabbitTest.Test_A2_3();
        }

        public void Test_A1_1()
        {
            byte[] key = StringToByteArray("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");

            RabbitCipher rabbit = new RabbitCipher(key);

            byte[] state_0 = StringToByteArray("B1 57 54 F0 36 A5 D6 EC F5 6B 45 26 1C 4A F7 02");
            byte[] state_1 = StringToByteArray("88 E8 D8 15 C5 9C 0C 39 7B 69 6C 47 89 C6 8A A7");
            byte[] state_2 = StringToByteArray("F4 16 A1 C3 70 0C D4 51 DA 68 D1 88 16 73 D6 96");

            rabbit.Round();
            byte[] S0 = rabbit.GetState();
            AssertEqual(S0, state_0);

            rabbit.Round();
            byte[] S1 = rabbit.GetState();
            AssertEqual(S1, state_1);

            rabbit.Round();
            byte[] S2 = rabbit.GetState();
            AssertEqual(S2, state_2);

            Console.WriteLine("Test A1/1 successful");
        }

        public void Test_A1_2()
        {
            byte[] key = StringToByteArray("91 28 13 29 2E 3D 36 FE 3B FC 62 F1 DC 51 C3 AC");

            RabbitCipher rabbit = new RabbitCipher(key);

            byte[] state_0 = StringToByteArray("3D 2D F3 C8 3E F6 27 A1 E9 7F C3 84 87 E2 51 9C");
            byte[] state_1 = StringToByteArray("F5 76 CD 61 F4 40 5B 88 96 BF 53 AA 85 54 FC 19");
            byte[] state_2 = StringToByteArray("E5 54 74 73 FB DB 43 50 8A E5 3B 20 20 4D 4C 5E");

            rabbit.Round();
            byte[] S0 = rabbit.GetState();
            AssertEqual(S0, state_0);

            rabbit.Round();
            byte[] S1 = rabbit.GetState();
            AssertEqual(S1, state_1);

            rabbit.Round();
            byte[] S2 = rabbit.GetState();
            AssertEqual(S2, state_2);

            Console.WriteLine("Test A1/2 successful");
        }

        public void Test_A1_3()
        {
            byte[] key = StringToByteArray("83 95 74 15 87 E0 C7 33 E9 E9 AB 01 C0 9B 00 43");

            RabbitCipher rabbit = new RabbitCipher(key);

            byte[] state_0 = StringToByteArray("0C B1 0D CD A0 41 CD AC 32 EB 5C FD 02 D0 60 9B");
            byte[] state_1 = StringToByteArray("95 FC 9F CA 0F 17 01 5A 7B 70 92 11 4C FF 3E AD");
            byte[] state_2 = StringToByteArray("96 49 E5 DE 8B FC 7F 3F 92 41 47 AD 3A 94 74 28");

            rabbit.Round();
            byte[] S0 = rabbit.GetState();
            AssertEqual(S0, state_0);

            rabbit.Round();
            byte[] S1 = rabbit.GetState();
            AssertEqual(S1, state_1);

            rabbit.Round();
            byte[] S2 = rabbit.GetState();
            AssertEqual(S2, state_2);

            Console.WriteLine("Test A1/3 successful");
        }

        public void Test_A2_1()
        {
            byte[] key = StringToByteArray("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
            byte[] iv = StringToByteArray("00 00 00 00 00 00 00 00");
            byte[] state_0 = StringToByteArray("C6 A7 27 5E F8 54 95 D8 7C CD 5D 37 67 05 B7 ED");
            byte[] state_1 = StringToByteArray("5F 29 A6 AC 04 F5 EF D4 7B 8F 29 32 70 DC 4A 8D");
            byte[] state_2 = StringToByteArray("2A DE 82 2B 29 DE 6C 1E E5 2B DB 8A 47 BF 8F 66");

            RabbitCipher rabbit = RabbitCipher.Create(key, iv);

            rabbit.Round();
            byte[] S0 = rabbit.GetState();
            AssertEqual(state_0, S0);

            rabbit.Round();
            byte[] S1 = rabbit.GetState();
            AssertEqual(state_1, S1);

            rabbit.Round();
            byte[] S2 = rabbit.GetState();
            AssertEqual(state_2, S2);

            Console.WriteLine("Test A2/1 successful");
        }

        public void Test_A2_2()
        {
            byte[] key = StringToByteArray("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
            byte[] iv = StringToByteArray("C3 73 F5 75 C1 26 7E 59");
            byte[] state_0 = StringToByteArray("1F CD 4E B9 58 00 12 E2 E0 DC CC 92 22 01 7D 6D");
            byte[] state_1 = StringToByteArray("A7 5F 4E 10 D1 21 25 01 7B 24 99 FF ED 93 6F 2E");
            byte[] state_2 = StringToByteArray("EB C1 12 C3 93 E7 38 39 23 56 BD D0 12 02 9B A7");

            RabbitCipher rabbit = RabbitCipher.Create(key, iv);

            rabbit.Round();
            byte[] S0 = rabbit.GetState();
            AssertEqual(state_0, S0);

            rabbit.Round();
            byte[] S1 = rabbit.GetState();
            AssertEqual(state_1, S1);

            rabbit.Round();
            byte[] S2 = rabbit.GetState();
            AssertEqual(state_2, S2);

            Console.WriteLine("Test A2/2 successful");
        }

        public void Test_A2_3()
        {
            byte[] key = StringToByteArray("00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");
            byte[] iv = StringToByteArray("A6 EB 56 1A D2 F4 17 27");
            byte[] state_0 = StringToByteArray("44 5A D8 C8 05 85 8D BF 70 B6 AF 23 A1 51 10 4D");
            byte[] state_1 = StringToByteArray("96 C8 F2 79 47 F4 2C 5B AE AE 67 C6 AC C3 5B 03");
            byte[] state_2 = StringToByteArray("9F CB FC 89 5F A7 1C 17 31 3D F0 34 F0 15 51 CB");

            RabbitCipher rabbit = RabbitCipher.Create(key, iv);

            rabbit.Round();
            byte[] S0 = rabbit.GetState();
            AssertEqual(state_0, S0);

            rabbit.Round();
            byte[] S1 = rabbit.GetState();
            AssertEqual(state_1, S1);

            rabbit.Round();
            byte[] S2 = rabbit.GetState();
            AssertEqual(state_2, S2);

            Console.WriteLine("Test A2/3 successful");
        }
    }

}

