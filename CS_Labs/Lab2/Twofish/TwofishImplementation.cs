﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Twofish
{
    public class TwofishImplementation : IDisposable
    {
        private const int BlockSize = 128; // number of bits per block
        private const int Rounds = 16; // default number of rounds for 128/192/256-bit keys
        private const int MaxKeyBits = 256; // max number of bits of key
        private const int InputWhiten = 0;
        private const int OutputWhiten = InputWhiten + BlockSize / 32;
        private const int RoundSubkeys = OutputWhiten + BlockSize / 32;
        private const int TotalSubkeys = RoundSubkeys + 2 * Rounds;
        private readonly CipherMode _cipherMode;
        private readonly DWord[] _iv;

        private readonly DWord[] _key;

        private readonly DWord[] _sBoxKeys = new DWord[MaxKeyBits / 64]; // key bits used for S-boxes
        private readonly DWord[] _subKeys = new DWord[TotalSubkeys]; // round subkeys, input/output whitening bits

        public TwofishImplementation(IReadOnlyList<uint> key, IReadOnlyList<uint> iv, CipherMode cipherMode)
        {
            _key = new DWord[key.Count];
            for (var i = 0; i < _key.Length; i++) _key[i] = (DWord) key[i];

            if (iv != null)
            {
                _iv = new DWord[iv.Count];
                for (var i = 0; i < _iv.Length; i++) _iv[i] = (DWord) iv[i];
            }

            _cipherMode = cipherMode;

            ReKey();
        }

        public void Dispose()
        {
            Array.Clear(_key, 0, _key.Length);
            if (_iv != null) Array.Clear(_iv, 0, _iv.Length);
            Array.Clear(_sBoxKeys, 0, _sBoxKeys.Length);
            Array.Clear(_subKeys, 0, _subKeys.Length);
        }

        #region ReKey

        private const int SubkeyStep = 0x02020202;
        private const int SubkeyBump = 0x01010101;
        private const int SubkeyRotateLeft = 9;

        /// <summary>
        ///     Initialize the Twofish key schedule from key32
        /// </summary>
        private void ReKey()
        {
            BuildMds(); // built only first time it is accessed

            var k32E = new DWord[_key.Length / 2];
            var k32O = new DWord[_key.Length / 2]; // even/odd key dwords

            var k64Cnt = _key.Length / 2;
            for (var i = 0; i < k64Cnt; i++)
            {
                // split into even/odd key dwords
                k32E[i] = _key[2 * i];
                k32O[i] = _key[2 * i + 1];
                _sBoxKeys[k64Cnt - 1 - i] =
                    ReedSolomonMdsEncode(k32E[i],
                        k32O[i]); // compute S-box keys using (12,8) Reed-Solomon code over GF(256)
            }

            const int subkeyCnt = RoundSubkeys + 2 * Rounds;
            var keyLen = _key.Length * 4 * 8;
            for (var i = 0; i < subkeyCnt / 2; i++)
            {
                // compute round subkeys for PHT
                var a = F32((DWord) (i * SubkeyStep), k32E, keyLen); // A uses even key dwords
                var b = F32((DWord) (i * SubkeyStep + SubkeyBump), k32O, keyLen); // B uses odd  key dwords
                b = RotateLeft(b, 8);
                _subKeys[2 * i] = a + b; // combine with a PHT
                _subKeys[2 * i + 1] = RotateLeft(a + 2 * b, SubkeyRotateLeft);
            }
        }

        #endregion

        #region Encrypt/decrypt

        /// <summary>
        ///     Encrypt block(s) of data using Twofish.
        /// </summary>
        internal void BlockEncrypt(byte[] inputBuffer, int inputOffset, byte[] outputBuffer, int outputBufferOffset)
        {
            var x = new DWord[BlockSize / 32];
            for (var i = 0; i < BlockSize / 32; i++)
            {
                // copy in the block, add whitening
                x[i] = new DWord(inputBuffer, inputOffset + i * 4) ^ _subKeys[InputWhiten + i];
                if (_cipherMode == CipherMode.CBC) x[i] ^= _iv[i];
            }

            var keyLen = _key.Length * 4 * 8;
            for (var r = 0; r < Rounds; r++)
            {
                // main Twofish encryption loop
                var t0 = F32(x[0], _sBoxKeys, keyLen);
                var t1 = F32(RotateLeft(x[1], 8), _sBoxKeys, keyLen);

                x[3] = RotateLeft(x[3], 1);
                x[2] ^= t0 + t1 + _subKeys[RoundSubkeys + 2 * r]; // PHT, round keys
                x[3] ^= t0 + 2 * t1 + _subKeys[RoundSubkeys + 2 * r + 1];
                x[2] = RotateRight(x[2], 1);

                if (r >= Rounds - 1) continue;

                // swap for next round
                var tmp = x[0];
                x[0] = x[2];
                x[2] = tmp;
                tmp = x[1];
                x[1] = x[3];
                x[3] = tmp;
            }

            for (var i = 0; i < BlockSize / 32; i++)
            {
                // copy out, with whitening
                var outValue = x[i] ^ _subKeys[OutputWhiten + i];
                outputBuffer[outputBufferOffset + i * 4 + 0] = outValue.B0;
                outputBuffer[outputBufferOffset + i * 4 + 1] = outValue.B1;
                outputBuffer[outputBufferOffset + i * 4 + 2] = outValue.B2;
                outputBuffer[outputBufferOffset + i * 4 + 3] = outValue.B3;
                if (_cipherMode == CipherMode.CBC) _iv[i] = outValue;
            }
        }

        /// <summary>
        ///     Decrypt block(s) of data using Twofish.
        /// </summary>
        internal void BlockDecrypt(byte[] inputBuffer, int inputOffset, byte[] outputBuffer, int outputBufferOffset)
        {
            var x = new DWord[BlockSize / 32];
            var input = new DWord[BlockSize / 32];
            for (var i = 0; i < BlockSize / 32; i++)
            {
                // copy in the block, add whitening
                input[i] = new DWord(inputBuffer, inputOffset + i * 4);
                x[i] = input[i] ^ _subKeys[OutputWhiten + i];
            }

            var keyLen = _key.Length * 4 * 8;
            for (var r = Rounds - 1; r >= 0; r--)
            {
                // main Twofish decryption loop
                var t0 = F32(x[0], _sBoxKeys, keyLen);
                var t1 = F32(RotateLeft(x[1], 8), _sBoxKeys, keyLen);

                x[2] = RotateLeft(x[2], 1);
                x[2] ^= t0 + t1 + _subKeys[RoundSubkeys + 2 * r]; // PHT, round keys
                x[3] ^= t0 + 2 * t1 + _subKeys[RoundSubkeys + 2 * r + 1];
                x[3] = RotateRight(x[3], 1);

                if (r <= 0) continue;

                // unswap, except for last round
                t0 = x[0];
                x[0] = x[2];
                x[2] = t0;
                t1 = x[1];
                x[1] = x[3];
                x[3] = t1;
            }

            for (var i = 0; i < BlockSize / 32; i++)
            {
                // copy out, with whitening
                x[i] ^= _subKeys[InputWhiten + i];
                if (_cipherMode == CipherMode.CBC)
                {
                    x[i] ^= _iv[i];
                    _iv[i] = input[i];
                }

                outputBuffer[outputBufferOffset + i * 4 + 0] = x[i].B0;
                outputBuffer[outputBufferOffset + i * 4 + 1] = x[i].B1;
                outputBuffer[outputBufferOffset + i * 4 + 2] = x[i].B2;
                outputBuffer[outputBufferOffset + i * 4 + 3] = x[i].B3;
            }
        }

        #endregion

        #region F32

        /// <summary>
        ///     Run four bytes through keyed S-boxes and apply MDS matrix.
        /// </summary>
        private static DWord F32(DWord x, IList<DWord> k32, int keyLen)
        {
            if (keyLen >= 256)
            {
                x.B0 = (byte) (P8X8[P04, x.B0] ^ k32[3].B0);
                x.B1 = (byte) (P8X8[P14, x.B1] ^ k32[3].B1);
                x.B2 = (byte) (P8X8[P24, x.B2] ^ k32[3].B2);
                x.B3 = (byte) (P8X8[P34, x.B3] ^ k32[3].B3);
            }

            if (keyLen >= 192)
            {
                x.B0 = (byte) (P8X8[P03, x.B0] ^ k32[2].B0);
                x.B1 = (byte) (P8X8[P13, x.B1] ^ k32[2].B1);
                x.B2 = (byte) (P8X8[P23, x.B2] ^ k32[2].B2);
                x.B3 = (byte) (P8X8[P33, x.B3] ^ k32[2].B3);
            }

            if (keyLen >= 128)
                x = MdsTable[0, P8X8[P01, P8X8[P02, x.B0] ^ k32[1].B0] ^ k32[0].B0]
                    ^ MdsTable[1, P8X8[P11, P8X8[P12, x.B1] ^ k32[1].B1] ^ k32[0].B1]
                    ^ MdsTable[2, P8X8[P21, P8X8[P22, x.B2] ^ k32[1].B2] ^ k32[0].B2]
                    ^ MdsTable[3, P8X8[P31, P8X8[P32, x.B3] ^ k32[1].B3] ^ k32[0].B3];

            return x;
        }

        private static DWord RotateLeft(DWord x, int n)
        {
            return (x << n) | (x >> (32 - n));
        }

        private static DWord RotateRight(DWord x, int n)
        {
            return (x >> n) | (x << (32 - n));
        }

        private const uint P01 = 0;
        private const uint P02 = 0;
        private const uint P03 = P01 ^ 1; // "extend" to larger key sizes
        private const uint P04 = 1;

        private const uint P11 = 0;
        private const uint P12 = 1;
        private const uint P13 = P11 ^ 1;
        private const uint P14 = 0;

        private const uint P21 = 1;
        private const uint P22 = 0;
        private const uint P23 = P21 ^ 1;
        private const uint P24 = 0;

        private const uint P31 = 1;
        private const uint P32 = 1;
        private const uint P33 = P31 ^ 1;
        private const uint P34 = 1;

        private static readonly byte[,] P8X8 =
        {
            {
                0xA9, 0x67, 0xB3, 0xE8, 0x04, 0xFD, 0xA3, 0x76,
                0x9A, 0x92, 0x80, 0x78, 0xE4, 0xDD, 0xD1, 0x38,
                0x0D, 0xC6, 0x35, 0x98, 0x18, 0xF7, 0xEC, 0x6C,
                0x43, 0x75, 0x37, 0x26, 0xFA, 0x13, 0x94, 0x48,
                0xF2, 0xD0, 0x8B, 0x30, 0x84, 0x54, 0xDF, 0x23,
                0x19, 0x5B, 0x3D, 0x59, 0xF3, 0xAE, 0xA2, 0x82,
                0x63, 0x01, 0x83, 0x2E, 0xD9, 0x51, 0x9B, 0x7C,
                0xA6, 0xEB, 0xA5, 0xBE, 0x16, 0x0C, 0xE3, 0x61,
                0xC0, 0x8C, 0x3A, 0xF5, 0x73, 0x2C, 0x25, 0x0B,
                0xBB, 0x4E, 0x89, 0x6B, 0x53, 0x6A, 0xB4, 0xF1,
                0xE1, 0xE6, 0xBD, 0x45, 0xE2, 0xF4, 0xB6, 0x66,
                0xCC, 0x95, 0x03, 0x56, 0xD4, 0x1C, 0x1E, 0xD7,
                0xFB, 0xC3, 0x8E, 0xB5, 0xE9, 0xCF, 0xBF, 0xBA,
                0xEA, 0x77, 0x39, 0xAF, 0x33, 0xC9, 0x62, 0x71,
                0x81, 0x79, 0x09, 0xAD, 0x24, 0xCD, 0xF9, 0xD8,
                0xE5, 0xC5, 0xB9, 0x4D, 0x44, 0x08, 0x86, 0xE7,
                0xA1, 0x1D, 0xAA, 0xED, 0x06, 0x70, 0xB2, 0xD2,
                0x41, 0x7B, 0xA0, 0x11, 0x31, 0xC2, 0x27, 0x90,
                0x20, 0xF6, 0x60, 0xFF, 0x96, 0x5C, 0xB1, 0xAB,
                0x9E, 0x9C, 0x52, 0x1B, 0x5F, 0x93, 0x0A, 0xEF,
                0x91, 0x85, 0x49, 0xEE, 0x2D, 0x4F, 0x8F, 0x3B,
                0x47, 0x87, 0x6D, 0x46, 0xD6, 0x3E, 0x69, 0x64,
                0x2A, 0xCE, 0xCB, 0x2F, 0xFC, 0x97, 0x05, 0x7A,
                0xAC, 0x7F, 0xD5, 0x1A, 0x4B, 0x0E, 0xA7, 0x5A,
                0x28, 0x14, 0x3F, 0x29, 0x88, 0x3C, 0x4C, 0x02,
                0xB8, 0xDA, 0xB0, 0x17, 0x55, 0x1F, 0x8A, 0x7D,
                0x57, 0xC7, 0x8D, 0x74, 0xB7, 0xC4, 0x9F, 0x72,
                0x7E, 0x15, 0x22, 0x12, 0x58, 0x07, 0x99, 0x34,
                0x6E, 0x50, 0xDE, 0x68, 0x65, 0xBC, 0xDB, 0xF8,
                0xC8, 0xA8, 0x2B, 0x40, 0xDC, 0xFE, 0x32, 0xA4,
                0xCA, 0x10, 0x21, 0xF0, 0xD3, 0x5D, 0x0F, 0x00,
                0x6F, 0x9D, 0x36, 0x42, 0x4A, 0x5E, 0xC1, 0xE0
            },
            {
                0x75, 0xF3, 0xC6, 0xF4, 0xDB, 0x7B, 0xFB, 0xC8,
                0x4A, 0xD3, 0xE6, 0x6B, 0x45, 0x7D, 0xE8, 0x4B,
                0xD6, 0x32, 0xD8, 0xFD, 0x37, 0x71, 0xF1, 0xE1,
                0x30, 0x0F, 0xF8, 0x1B, 0x87, 0xFA, 0x06, 0x3F,
                0x5E, 0xBA, 0xAE, 0x5B, 0x8A, 0x00, 0xBC, 0x9D,
                0x6D, 0xC1, 0xB1, 0x0E, 0x80, 0x5D, 0xD2, 0xD5,
                0xA0, 0x84, 0x07, 0x14, 0xB5, 0x90, 0x2C, 0xA3,
                0xB2, 0x73, 0x4C, 0x54, 0x92, 0x74, 0x36, 0x51,
                0x38, 0xB0, 0xBD, 0x5A, 0xFC, 0x60, 0x62, 0x96,
                0x6C, 0x42, 0xF7, 0x10, 0x7C, 0x28, 0x27, 0x8C,
                0x13, 0x95, 0x9C, 0xC7, 0x24, 0x46, 0x3B, 0x70,
                0xCA, 0xE3, 0x85, 0xCB, 0x11, 0xD0, 0x93, 0xB8,
                0xA6, 0x83, 0x20, 0xFF, 0x9F, 0x77, 0xC3, 0xCC,
                0x03, 0x6F, 0x08, 0xBF, 0x40, 0xE7, 0x2B, 0xE2,
                0x79, 0x0C, 0xAA, 0x82, 0x41, 0x3A, 0xEA, 0xB9,
                0xE4, 0x9A, 0xA4, 0x97, 0x7E, 0xDA, 0x7A, 0x17,
                0x66, 0x94, 0xA1, 0x1D, 0x3D, 0xF0, 0xDE, 0xB3,
                0x0B, 0x72, 0xA7, 0x1C, 0xEF, 0xD1, 0x53, 0x3E,
                0x8F, 0x33, 0x26, 0x5F, 0xEC, 0x76, 0x2A, 0x49,
                0x81, 0x88, 0xEE, 0x21, 0xC4, 0x1A, 0xEB, 0xD9,
                0xC5, 0x39, 0x99, 0xCD, 0xAD, 0x31, 0x8B, 0x01,
                0x18, 0x23, 0xDD, 0x1F, 0x4E, 0x2D, 0xF9, 0x48,
                0x4F, 0xF2, 0x65, 0x8E, 0x78, 0x5C, 0x58, 0x19,
                0x8D, 0xE5, 0x98, 0x57, 0x67, 0x7F, 0x05, 0x64,
                0xAF, 0x63, 0xB6, 0xFE, 0xF5, 0xB7, 0x3C, 0xA5,
                0xCE, 0xE9, 0x68, 0x44, 0xE0, 0x4D, 0x43, 0x69,
                0x29, 0x2E, 0xAC, 0x15, 0x59, 0xA8, 0x0A, 0x9E,
                0x6E, 0x47, 0xDF, 0x34, 0x35, 0x6A, 0xCF, 0xDC,
                0x22, 0xC9, 0xC0, 0x9B, 0x89, 0xD4, 0xED, 0xAB,
                0x12, 0xA2, 0x0D, 0x52, 0xBB, 0x02, 0x2F, 0xA9,
                0xD7, 0x61, 0x1E, 0xB4, 0x50, 0x04, 0xF6, 0xC2,
                0x16, 0x25, 0x86, 0x56, 0x55, 0x09, 0xBE, 0x91
            }
        };

        private static readonly DWord[,] MdsTable = new DWord[4, 256];
        private static bool _mdsTableBuilt;
        private static readonly object BuildMdsSyncLock = new object();

        private static void BuildMds()
        {
            lock (BuildMdsSyncLock)
            {
                if (_mdsTableBuilt) return;

                var m1 = new byte[2];
                var mX = new byte[2];
                var mY = new byte[4];

                for (var i = 0; i < 256; i++)
                {
                    m1[0] = P8X8[0, i]; // compute all the matrix elements 
                    mX[0] = (byte) Mx_X(m1[0]);
                    mY[0] = (byte) Mx_Y(m1[0]);

                    m1[1] = P8X8[1, i];
                    mX[1] = (byte) Mx_X(m1[1]);
                    mY[1] = (byte) Mx_Y(m1[1]);

                    MdsTable[0, i].B0 = m1[1];
                    MdsTable[0, i].B1 = mX[1];
                    MdsTable[0, i].B2 = mY[1];
                    MdsTable[0, i].B3 = mY[1]; // SetMDS(0);

                    MdsTable[1, i].B0 = mY[0];
                    MdsTable[1, i].B1 = mY[0];
                    MdsTable[1, i].B2 = mX[0];
                    MdsTable[1, i].B3 = m1[0]; // SetMDS(1);

                    MdsTable[2, i].B0 = mX[1];
                    MdsTable[2, i].B1 = mY[1];
                    MdsTable[2, i].B2 = m1[1];
                    MdsTable[2, i].B3 = mY[1]; // SetMDS(2);

                    MdsTable[3, i].B0 = mX[0];
                    MdsTable[3, i].B1 = m1[0];
                    MdsTable[3, i].B2 = mY[0];
                    MdsTable[3, i].B3 = mX[0]; // SetMDS(3);
                }

                _mdsTableBuilt = true;
            }
        }

        #endregion

        #region Reed-Solomon

        private const uint RsGfFdbk = 0x14D; //field generator

        /// <summary>
        ///     Use (12,8) Reed-Solomon code over GF(256) to produce a key S-box dword from two key material dwords.
        /// </summary>
        /// <param name="k0">1st dword</param>
        /// <param name="k1">2nd dword</param>
        private static DWord ReedSolomonMdsEncode(DWord k0, DWord k1)
        {
            var r = new DWord();
            for (var i = 0; i < 2; i++)
            {
                r ^= i > 0 ? k0 : k1; // merge in 32 more key bits
                for (var j = 0; j < 4; j++)
                {
                    // shift one byte at a time 
                    var b = (byte) (r >> 24);
                    var g2 = (byte) ((b << 1) ^ ((b & 0x80) > 0 ? RsGfFdbk : 0));
                    var g3 = (byte) (((b >> 1) & 0x7F) ^ ((b & 1) > 0 ? RsGfFdbk >> 1 : 0) ^ g2);
                    r.B3 = (byte) (r.B2 ^ g3);
                    r.B2 = (byte) (r.B1 ^ g2);
                    r.B1 = (byte) (r.B0 ^ g3);
                    r.B0 = b;
                }
            }

            return r;
        }

        private static uint Mx_X(uint x)
        {
            return x ^ Lfsr2(x); // 5B
        }

        private static uint Mx_Y(uint x)
        {
            return x ^ Lfsr1(x) ^ Lfsr2(x); // EF
        }

        private const uint MdsGfFdbk = 0x169; // primitive polynomial for GF(256)

        private static uint Lfsr1(uint x)
        {
            return (x >> 1) ^ ((x & 0x01) > 0 ? MdsGfFdbk / 2 : 0);
        }

        private static uint Lfsr2(uint x)
        {
            return (x >> 2) ^ ((x & 0x02) > 0 ? MdsGfFdbk / 2 : 0)
                            ^ ((x & 0x01) > 0 ? MdsGfFdbk / 4 : 0);
        }

        #endregion
    }
}