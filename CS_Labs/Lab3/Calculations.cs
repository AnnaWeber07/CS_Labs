using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text;

namespace RsaAlgorithm
{
    public class Calculations
    {
        public bool IsSimple(long n)
        {
            if (n < 2)
            {
                return false;
            }

            if (n == 2)
            {
                return true;
            }

            for (long i = 2; i < n; i++)
            {
                if (n % i == 0)
                    return false;
            }

            return true;
        }

        public long CalculateD(long m)
        {
            long d = m - 1;
            for (long i = 2; i <= m; i++)
            {
                if ((m % i == 0) && (d % i == 0))
                {
                    d--;
                    i = 1;
                }
            }

            return d;
        }

        public long CalculateE(long d, long m)
        {
            long e = 10;

            while (true)
            {
                if ((e * d) % m == 1)
                {
                    break;
                }
                else
                {
                    e++;
                }
            }

            return e;
        }

        public void ShowResult(List<string> res)
        {
            for (int i = 0; i < res.Count; i++)
            {
                Console.WriteLine(res[i]);
            }
        }

    }
}
