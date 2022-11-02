using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace RsaAlgorithm
{
    public class Encryption
    {
        public long p;
        public long q;
        public long m;
        public long d;
        public long n;
        public long e_;
        public string todo;
        public List<string> result = new List<string>();

        Calculations calculations = new Calculations();
        readonly Alphabet alphabet = new Alphabet();

        public Encryption(long p, long q, string todo)
        {
            this.p = p;
            this.q = q;
            this.todo = todo;
        }

        public void EncryptionProcess()
        {
            Encrypt(p, q);
            Console.Write("N: ");
            Console.WriteLine(n.ToString());
            Console.Write("D: ");
            Console.WriteLine(d.ToString());
            Console.Write("E: ");
            Console.WriteLine(e_.ToString());
            //RsaEncrypt(todo, e_, n);
            calculations.ShowResult(result);

        }

        public void Encrypt(long p, long q)
        {
            if (calculations.IsSimple(p) && calculations.IsSimple(q))
            {
                todo = todo.ToUpper();

                n = p * q;
                m = (p - 1) * (q - 1);
                d = calculations.CalculateD(m);
                e_ = calculations.CalculateE(d, m);

                RsaEncrypt(todo, e_, n);

                //foreach (string item in result)
                //    Console.WriteLine(item);

                d.ToString();
                n.ToString();
            }
            else
                Console.WriteLine("P and/or Q aren't prime numbers :( \n Try again!");

        }

        public List<string> RsaEncrypt(string s, long e, long n)
        {
            BigInteger bi;

            for (int i = 0; i < s.Length; i++)
            {
                int index = Array.IndexOf(alphabet.alphabetCharacters, s[i]);

                bi = new BigInteger(index);
                bi = BigInteger.Pow(bi, (int)e);

                BigInteger n_ = new BigInteger((int)n);

                bi = bi % n_;

                result.Add(bi.ToString());
            }

            return result;
        }
    }
}
