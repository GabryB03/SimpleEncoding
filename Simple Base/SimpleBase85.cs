﻿using System.IO;
using System.Text;
using System;

namespace SimpleEncoding
{
    internal class SimpleBase85 : SimpleBase
    {
        public const string DefaultAlphabet = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstu";
        public const char DefaultSpecial = (char)0;
        private static uint[] Pow85 = { 85 * 85 * 85 * 85, 85 * 85 * 85, 85 * 85, 85, 1 };

        public const string Prefix = "<~";
        public const string Postfix = "~>";

        public override bool HaveSpecial
        {
            get { return false; }
        }

        public bool PrefixPostfix
        {
            get;
            set;
        }

        public SimpleBase85(string alphabet = DefaultAlphabet, char special = DefaultSpecial, bool prefixPostfix = false, Encoding textEncoding = null)
            : base(85, alphabet, special, textEncoding)
        {
            PrefixPostfix = prefixPostfix;
            BlockBitsCount = 32;
            BlockCharsCount = 5;
        }

        public override string Encode(byte[] data)
        {
            unchecked
            {
                byte[] encodedBlock = new byte[5];
                int decodedBlockLength = 4;
                int resultLength = (int)(data.Length * (encodedBlock.Length / decodedBlockLength));
                if (PrefixPostfix)
                    resultLength += Prefix.Length + Postfix.Length;
                StringBuilder sb = new StringBuilder(resultLength);

                if (PrefixPostfix)
                    sb.Append(Prefix);

                int count = 0;
                uint tuple = 0;
                foreach (byte b in data)
                {
                    if (count >= decodedBlockLength - 1)
                    {
                        tuple |= b;
                        if (tuple == 0)
                            sb.Append('z');
                        else
                            EncodeBlock(encodedBlock.Length, sb, encodedBlock, tuple);
                        tuple = 0;
                        count = 0;
                    }
                    else
                    {
                        tuple |= (uint)(b << (24 - (count * 8)));
                        count++;
                    }
                }

                if (count > 0)
                    EncodeBlock(count + 1, sb, encodedBlock, tuple);

                if (PrefixPostfix)
                    sb.Append(Postfix);

                return sb.ToString();
            }
        }

        public override byte[] Decode(string data)
        {
            unchecked
            {
                string dataWithoutPrefixPostfix = data;
                if (PrefixPostfix)
                {
                    if (!dataWithoutPrefixPostfix.StartsWith(Prefix) || !dataWithoutPrefixPostfix.EndsWith(Postfix))
                    {
                        throw new Exception("ASCII85 encoded data should begin with '" + Prefix + "' and end with '" + Postfix + "'");
                    }
                    dataWithoutPrefixPostfix = dataWithoutPrefixPostfix.Substring(Prefix.Length, dataWithoutPrefixPostfix.Length - Prefix.Length - Postfix.Length);
                }

                MemoryStream ms = new MemoryStream();
                int count = 0;
                bool processChar = false;

                uint tuple = 0;
                int encidedBlockLength = 5;
                byte[] decodedBlock = new byte[4];
                foreach (char c in dataWithoutPrefixPostfix)
                {
                    switch (c)
                    {
                        case 'z':
                            if (count != 0)
                            {
                                throw new Exception("The character 'z' is invalid inside an ASCII85 block.");
                            }
                            decodedBlock[0] = 0;
                            decodedBlock[1] = 0;
                            decodedBlock[2] = 0;
                            decodedBlock[3] = 0;
                            ms.Write(decodedBlock, 0, decodedBlock.Length);
                            processChar = false;
                            break;
                        default:
                            processChar = true;
                            break;
                    }

                    if (processChar)
                    {
                        tuple += (uint)InvAlphabet[c] * Pow85[count];
                        count++;
                        if (count == encidedBlockLength)
                        {
                            DecodeBlock(decodedBlock.Length, decodedBlock, tuple);
                            ms.Write(decodedBlock, 0, decodedBlock.Length);
                            tuple = 0;
                            count = 0;
                        }
                    }
                }

                if (count != 0)
                {
                    if (count == 1)
                    {
                        throw new Exception("The last block of ASCII85 data cannot be a single byte.");
                    }
                    count--;
                    tuple += Pow85[count];
                    DecodeBlock(count, decodedBlock, tuple);
                    for (int i = 0; i < count; i++)
                    {
                        ms.WriteByte(decodedBlock[i]);
                    }
                }

                return ms.ToArray();
            }
        }

        private void EncodeBlock(int count, StringBuilder sb, byte[] encodedBlock, uint tuple)
        {
            unchecked
            {
                for (int i = encodedBlock.Length - 1; i >= 0; i--)
                {
                    encodedBlock[i] = (byte)(tuple % 85);
                    tuple /= 85;
                }

                for (int i = 0; i < count; i++)
                    sb.Append(Alphabet[encodedBlock[i]]);
            }
        }

        private void DecodeBlock(int bytes, byte[] decodedBlock, uint tuple)
        {
            unchecked
            {
                for (int i = 0; i < bytes; i++)
                    decodedBlock[i] = (byte)(tuple >> 24 - (i * 8));
            }
        }
    }
}    