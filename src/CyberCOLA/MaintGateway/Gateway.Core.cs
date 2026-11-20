using System.Buffers;
using System.Buffers.Binary;

namespace Com.Cybersoft4u.Service.WebAPI.MaintGateway.Gateway.Core;

/// <summary>
/// Provides utility methods for packing and parsing binary messages using a 2-byte length prefix.
/// </summary>
public static class BinaryProtocol
{
    /// <summary>
    /// Packs the specified payload by prepending a 2-byte big-endian length prefix.
    /// </summary>
    /// <param name="payload">The raw data bytes to pack.</param>
    /// <returns>A read-only memory buffer containing the length-prefixed packet.</returns>
    /// <exception cref="InvalidOperationException">Thrown if packing fails.</exception>
    public static ReadOnlyMemory<byte> Pack(byte[] payload)
    {
        try
        {
            var buffer = new byte[payload.Length + 2];
            BinaryPrimitives.WriteInt16BigEndian(buffer.AsSpan(0, 2), (short)payload.Length);
            payload.CopyTo(buffer.AsMemory(2));
            return buffer;
        }
        catch (Exception ex)
        {
            // In static methods without a logger, exceptions are usually thrown to be logged by the caller
            throw new InvalidOperationException("Failed to pack binary message", ex);
        }
    }

    /// <summary>
    /// Attempts to parse a complete packet from the provided sequence buffer.
    /// </summary>
    /// <param name="buffer">The input sequence buffer. If a packet is parsed, the buffer is advanced.</param>
    /// <param name="message">When this method returns, contains the parsed payload if successful; otherwise, null.</param>
    /// <returns><c>true</see> if a complete packet was successfully parsed; otherwise, <c>false</see>.</returns>
    /// <exception cref="InvalidDataException">Thrown if an invalid message length is detected.</exception>
    public static bool TryParse(ref ReadOnlySequence<byte> buffer, out byte[]? message)
    {
        message = null;
        try
        {
            if (buffer.Length < 2) return false;

            Span<byte> lengthBytes = stackalloc byte[2];
            buffer.Slice(0, 2).CopyTo(lengthBytes);
            int length = BinaryPrimitives.ReadInt16BigEndian(lengthBytes);

            if (length < 0 || length > 1024 * 1024) // Added maximum length protection (1MB)
            {
                throw new InvalidDataException($"Invalid message length detected: {length}");
            }

            if (buffer.Length < 2 + length) return false;

            message = buffer.Slice(2, length).ToArray();
            buffer = buffer.Slice(2 + length);
            return true;
        }
        catch (Exception)
        {
            // If a parsing error occurs (e.g., data corruption), clear the buffer to avoid infinite loops
            buffer = buffer.Slice(buffer.Length);
            throw;
        }
    }
}
