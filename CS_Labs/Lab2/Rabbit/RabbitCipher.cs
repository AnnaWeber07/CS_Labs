using System;
using System.Runtime.CompilerServices;



namespace Crypto.Rabbit
{
    static class RotateOperator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt32 Rol(this UInt32 value, int shift)
            => (value << shift) | (value >> (32 - shift));
    }

    public class RabbitCipher
    {
        private byte[] Key = new byte[16];
        private UInt16[] K = new UInt16[8];
        private UInt32[] X = new UInt32[8];
        private UInt64[] C = new UInt64[8];
        private UInt64 CarryBit = 0;
        private const UInt64 WORDSIZE = 0x100000000;
        private UInt16[] S = new UInt16[8];
        private int StateIndex = -1;
        private UInt32[] G = new UInt32[8];

        private readonly UInt64[] A = new UInt64[]
        {
            0x4D34D34D, 0xD34D34D3,
            0x34D34D34, 0x4D34D34D,
            0xD34D34D3, 0x34D34D34,
            0x4D34D34D, 0xD34D34D3
        };

        static UInt64 ArrayToUInt64(byte[] b)
        {
            if (b.Length != 8)
                throw new ArgumentException("IV length must be 8 bytes");

            UInt64 val = 0;
            for (int j = 0; j < b.Length; j++)
            {
                val = val << 8 | b[j];
            }
            return val;
        }

        public static RabbitCipher Create(byte[] key, byte[] IV)
        {
            if (key.Length != 16)
                throw new ArgumentException("Keylength must be 16 bytes", "key");
            if (IV.Length != 8)
                throw new ArgumentException("IV length must be 8 bytes", "IV");

            return new RabbitCipher(key, ArrayToUInt64(IV));
        }

        public static RabbitEncryptor CreateEncryptor(byte[] key)
        {
            return new RabbitEncryptor(new RabbitCipher(key));
        }

        public static RabbitEncryptor CreateEncryptor(byte[] key, byte[] IV)
        {
            return new RabbitEncryptor(Create(key, IV));
        }

        public RabbitCipher(byte[] key, byte[] IV) : this(key, ArrayToUInt64(IV))
        {
        }

        public RabbitCipher(byte[] key, UInt64? IV = null)
        {
            if (key.Length != 16)
                throw new ArgumentException("Keylength must be 16 bytes", "key");

            Array.Copy(key, Key, 16);

            for (int i = 0; i < 8; i++)
            {
                K[i] = (ushort)((key[15 - 2 * i - 1] << 8) | key[15 - 2 * i]);
            }

            for (int j = 0; j < 8; j++)
            {
                if (j % 2 == 0)
                {
                    X[j] = (UInt32)(K[(j + 1) % 8] << 16) | K[j];
                    C[j] = (UInt32)(K[(j + 4) % 8] << 16) | K[(j + 5) % 8];
                }
                else
                {
                    X[j] = (UInt32)(K[(j + 5) % 8] << 16) | K[(j + 4) % 8];
                    C[j] = (UInt32)(K[j] << 16) | K[(j + 1) % 8];
                }
            }

            for (int i = 0; i < 4; i++)
            {
                Round();
            }

            for (int j = 0; j < 8; j++)
            {
                C[j] = C[j] ^ X[(j + 4) % 8];
            }

            // IV

            if (IV != null)
            {
                C[0] = C[0] ^ (UInt32)(IV & 0xFFFFFFFF);
                C[1] = C[1] ^ (UInt32)(((IV & 0xFFFF000000000000) >> 32) | ((IV & 0xFFFF0000) >> 16));
                C[2] = C[2] ^ (UInt32)((IV & 0xFFFFFFFF00000000) >> 32);
                C[3] = C[3] ^ (UInt32)(((IV & 0xFFFF00000000) >> 16) | (IV & 0xFFFF));
                C[4] = C[4] ^ (UInt32)(IV & 0xFFFFFFFF);
                C[5] = C[5] ^ (UInt32)(((IV & 0xFFFF000000000000) >> 32) | ((IV & 0xFFFF0000) >> 16));
                C[6] = C[6] ^ (UInt32)((IV & 0xFFFFFFFF00000000) >> 32);
                C[7] = C[7] ^ (UInt32)(((IV & 0xFFFF00000000) >> 16) | (IV & 0xFFFF));

                for (int i = 0; i < 4; i++)
                {
                    Round();
                }
            }
        }

        public void Round()
        {
            for (int j = 0; j < 8; j++)
            {
                // Counter update
                UInt64 temp = C[j] + A[j] + CarryBit;
                CarryBit = (temp / WORDSIZE);
                C[j] = (temp % WORDSIZE);

                // Next state
                UInt64 t = ((UInt64)X[j] + C[j]) % WORDSIZE;
                UInt64 w = t * t;
                UInt32 lsw = (UInt32)(w & 0xFFFFFFFF);
                UInt32 msw = (UInt32)((w >> 32) & 0xFFFFFFFF);
                G[j] = lsw ^ msw;
            }

            X[0] = (UInt32)((G[0] + G[7].Rol(16) + G[6].Rol(16)) % WORDSIZE);
            X[1] = (UInt32)((G[1] + G[0].Rol(8) + G[7]) % WORDSIZE);
            X[2] = (UInt32)((G[2] + G[1].Rol(16) + G[0].Rol(16)) % WORDSIZE);
            X[3] = (UInt32)((G[3] + G[2].Rol(8) + G[1]) % WORDSIZE);
            X[4] = (UInt32)((G[4] + G[3].Rol(16) + G[2].Rol(16)) % WORDSIZE);
            X[5] = (UInt32)((G[5] + G[4].Rol(8) + G[3]) % WORDSIZE);
            X[6] = (UInt32)((G[6] + G[5].Rol(16) + G[4].Rol(16)) % WORDSIZE);
            X[7] = (UInt32)((G[7] + G[6].Rol(8) + G[5]) % WORDSIZE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Extract()
        {
            S[7] = (UInt16)((X[0]) ^ (X[5] >> 16));
            S[6] = (UInt16)((X[0] >> 16) ^ (X[3]));
            S[5] = (UInt16)((X[2]) ^ (X[7] >> 16));
            S[4] = (UInt16)((X[2] >> 16) ^ (X[5]));
            S[3] = (UInt16)((X[4]) ^ (X[1] >> 16));
            S[2] = (UInt16)((X[4] >> 16) ^ (X[7]));
            S[1] = (UInt16)((X[6]) ^ (X[3] >> 16));
            S[0] = (UInt16)((X[6] >> 16) ^ (X[1]));
        }

        public byte[] GetState()
        {
            Extract();
            byte[] T = new byte[16];
            for (int i = 0; i < 8; i++)
            {
                T[2 * i] = (byte)((S[i] & 0xFF00) >> 8);
                T[2 * i + 1] = (byte)(S[i] & 0xFF);
            }
            return T;
        }

        public byte CipherByte(byte b)
        {
            if (StateIndex == -1 || StateIndex > 15)
            {
                Round();
                Extract();
                StateIndex = 0;
            }

            byte U = StateIndex % 2 == 0 ? (byte)(S[StateIndex / 2] >> 8) : (byte)(S[StateIndex / 2] & 0xFF);
            byte value = (byte)(b ^ U);
            StateIndex++;
            return value;
        }

        public int CipherBlock(byte[] input, int inputOffset, int inputCount, byte[] output, int outputOffset)
        {
            int j = 0;

            while (j < inputCount && StateIndex != 16)
            {
                output[outputOffset + j] = CipherByte(input[inputOffset + j]);
                j++;
            }

            for (; j < inputCount - (inputCount % 16); j += 16)
            {
                Round();
                /**
                 * Optimization: Leave out Extract() here. This is possible as we are consuming complete blocks
                 * and need not worry about the following step, which will create a new block anyway.
                 **/
                output[outputOffset + j] = (byte)(input[inputOffset + j] ^ ((((X[6]) >> 16) ^ (X[1])) >> 8));
                output[outputOffset + j + 1] = (byte)(input[inputOffset + j + 1] ^ ((((X[6]) >> 16) ^ (X[1]))));
                output[outputOffset + j + 2] = (byte)(input[inputOffset + j + 2] ^ (((X[6]) ^ ((X[3]) >> 16)) >> 8));
                output[outputOffset + j + 3] = (byte)(input[inputOffset + j + 3] ^ (((X[6]) ^ ((X[3]) >> 16))));
                output[outputOffset + j + 4] = (byte)(input[inputOffset + j + 4] ^ ((((X[4]) >> 16) ^ (X[7])) >> 8));
                output[outputOffset + j + 5] = (byte)(input[inputOffset + j + 5] ^ ((((X[4]) >> 16) ^ (X[7]))));
                output[outputOffset + j + 6] = (byte)(input[inputOffset + j + 6] ^ (((X[4]) ^ ((X[1]) >> 16)) >> 8));
                output[outputOffset + j + 7] = (byte)(input[inputOffset + j + 7] ^ (((X[4]) ^ ((X[1]) >> 16))));
                output[outputOffset + j + 8] = (byte)(input[inputOffset + j + 8] ^ ((((X[2]) >> 16) ^ (X[5])) >> 8));
                output[outputOffset + j + 9] = (byte)(input[inputOffset + j + 9] ^ ((((X[2]) >> 16) ^ (X[5]))));
                output[outputOffset + j + 10] = (byte)(input[inputOffset + j + 10] ^ (((X[2]) ^ ((X[7]) >> 16)) >> 8));
                output[outputOffset + j + 11] = (byte)(input[inputOffset + j + 11] ^ (((X[2]) ^ ((X[7]) >> 16))));
                output[outputOffset + j + 12] = (byte)(input[inputOffset + j + 12] ^ ((((X[0]) >> 16) ^ (X[3])) >> 8));
                output[outputOffset + j + 13] = (byte)(input[inputOffset + j + 13] ^ ((((X[0]) >> 16) ^ (X[3]))));
                output[outputOffset + j + 14] = (byte)(input[inputOffset + j + 14] ^ (((X[0]) ^ ((X[5]) >> 16)) >> 8));
                output[outputOffset + j + 15] = (byte)(input[inputOffset + j + 15] ^ (((X[0]) ^ ((X[5]) >> 16))));
            }

            while (j < inputCount)
            {
                output[outputOffset + j] = CipherByte(input[inputOffset + j]);
                j++;
            }

            return inputCount;
        }

        public void CipherBlock(byte[] data, byte[] text)
        {
            CipherBlock(data, 0, data.Length, text, 0);
        }

        public byte[] CipherBlock(byte[] data)
        {
            byte[] text = new byte[data.Length];
            CipherBlock(data, 0, data.Length, text, 0);
            return text;
        }

        public byte[] CipherBlock(byte[] input, int inputOffset, int inputCount)
        {
            byte[] output = new byte[inputCount];
            CipherBlock(input, inputOffset, inputCount, output, 0);
            return output;
        }

    }
}
