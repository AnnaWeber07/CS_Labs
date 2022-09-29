using System;
using System.Collections.Generic;
using System.Text;

namespace CS_Labs
{
    public class CaesarCipher
    {
        public static char Cipher(char c, int key)
        {
            if (!char.IsLetter(c))
            {
                return c;
            }

            char d = char.IsUpper(c) ? 'A' : 'a';
            return (char)(((c + key - d) % 26) + d);
        }

        public static string Encryption(string input, int key)
        {
            string output = "";

            foreach (char c in input)
            {
                output += Cipher(c, key);
            }

            return output;
        }

        public static string Decryption(string input, int key)
        {
            return Encryption(input, 26 - key);
        }
    }
}
