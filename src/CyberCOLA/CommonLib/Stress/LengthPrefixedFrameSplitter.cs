using System;
using System.Collections.Generic;

namespace Com.Cybersoft4u.CommonLib.Stress
{
    /// <summary>
    /// Splits a TCP read into frames of [2-byte big-endian length][body].
    /// TCP may deliver part of a frame, or several frames, in one read; the unfinished tail is
    /// returned so the caller can prepend it to the next read instead of dropping it.
    /// </summary>
    public static class LengthPrefixedFrameSplitter
    {
        public static List<byte[]> Split(byte[] buffer, out byte[] remainder)
        {
            var frames = new List<byte[]>();
            var offset = 0;
            while (offset + 2 <= buffer.Length)
            {
                var bodyLength = buffer[offset] * 256 + buffer[offset + 1];
                if (buffer.Length < offset + 2 + bodyLength)
                {
                    break;
                }

                frames.Add(buffer[offset..(offset + 2 + bodyLength)]);
                offset += 2 + bodyLength;
            }

            // Covers both a partial body and a length header cut after its first byte.
            remainder = offset < buffer.Length ? buffer[offset..] : null;
            return frames;
        }
    }
}
