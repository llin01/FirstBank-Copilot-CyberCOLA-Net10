using System;
using System.Text;

namespace Com.Cybersoft4u.TM.ISO8583.Poc
{
    /// <summary>
    /// Proof of Concept (PoC) demonstrating how to refactor TM's transaction parsing 
    /// using ReadOnlySpan and Span-based APIs to achieve zero-allocation and extremely fast message decoding.
    /// </summary>
    public ref struct TmSpanParser
    {
        private readonly ReadOnlySpan<byte> _rawMessage;

        public TmSpanParser(ReadOnlySpan<byte> rawMessage)
        {
            _rawMessage = rawMessage;
        }

        /// <summary>
        /// Extracts a specific message field from the raw buffer without string allocation.
        /// </summary>
        public ReadOnlySpan<byte> GetField(int offset, int length)
        {
            if (offset < 0 || offset + length > _rawMessage.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Request slices out of raw message boundaries.");
            }
            return _rawMessage.Slice(offset, length);
        }

        /// <summary>
        /// Checks response code (Field 39) directly in binary, preventing string allocation.
        /// </summary>
        public bool IsResponseSuccess()
        {
            // Assuming Field 39 response code starts at offset 120 with length 2
            ReadOnlySpan<byte> responseField = GetField(120, 2);

            // Directly compare with UTF-8 byte representation of "00" (represented as "00"u8 in C# 11)
            // This is checked at compile-time and has zero allocation at runtime
            return responseField.SequenceEqual("00"u8);
        }

        /// <summary>
        /// Example demonstrating how to decode BCD (Binary Coded Decimal) fields directly using Spans.
        /// </summary>
        public static int DecodeBcd(ReadOnlySpan<byte> bcdSpan)
        {
            int result = 0;
            for (int i = 0; i < bcdSpan.Length; i++)
            {
                byte b = bcdSpan[i];
                int highNibble = (b >> 4) & 0x0F;
                int lowNibble = b & 0x0F;
                
                result = result * 10 + highNibble;
                result = result * 10 + lowNibble;
            }
            return result;
        }
    }
}
