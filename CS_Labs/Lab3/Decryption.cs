using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace RsaAlgorithm
{
    public class Decryption
    {
        Encryption encryption;
        Alphabet alphabet = new Alphabet();
        List<string> ciphertext = new List<string>();
        public string decrypted;

        public Decryption(Encryption encryption)
        {
            this.encryption = encryption;
        }

        public void DecryptionProcess()
        {
            Decrypt(encryption);
        }


        public void Decrypt(Encryption encryption)
        {
            foreach (string item in encryption.result)
            {
                ciphertext.Add(item);
            }

            decrypted = RsaDecrypt(ciphertext, encryption.d, encryption.n);
            Console.WriteLine(decrypted);
        }

        private string RsaDecrypt(List<string> input, long d, long n)
        {

            string result = "";

            BigInteger bi;

            foreach (string item in input)
            {
                bi = new BigInteger(Convert.ToDouble(item));
                bi = BigInteger.Pow(bi, (int)d);

                BigInteger n_ = new BigInteger((int)n);

                bi %= n_;

                int index = Convert.ToInt32(bi.ToString());

                result += alphabet.alphabetCharacters[index].ToString();
            }

            return result;
        }
    }
}
