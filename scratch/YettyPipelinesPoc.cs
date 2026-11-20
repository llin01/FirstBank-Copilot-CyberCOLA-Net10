using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Com.Cybersoft4u.Yetty.Channel.TCP.Poc
{
    /// <summary>
    /// Proof of Concept (PoC) demonstrating how to refactor Yetty's TCP receiver 
    /// using System.IO.Pipelines to achieve high-performance, asynchronous streaming.
    /// </summary>
    public class YettyPipelinesReceiver
    {
        private readonly Socket _socket;
        private readonly Pipe _pipe;

        public YettyPipelinesReceiver(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            // Initialize Pipe which internally manages memory buffer reuse using MemoryPool<byte>.Shared
            _pipe = new Pipe();
        }

        public PipeReader Reader => _pipe.Reader;

        /// <summary>
        /// Reads asynchronously from the TCP socket and writes incoming data into the Pipe (Producer).
        /// </summary>
        public async Task FillPipeAsync(CancellationToken cancellationToken)
        {
            PipeWriter writer = _pipe.Writer;
            const int minimumBufferSize = 512;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Allocate memory from the PipeWriter's internal pool
                Memory<byte> memory = writer.GetMemory(minimumBufferSize);

                try
                {
                    // Asynchronously receive from Socket directly into Pipe's memory chunk
                    int bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None, cancellationToken);
                    if (bytesRead == 0)
                    {
                        // Socket was closed by remote peer
                        break;
                    }

                    // Advance the writer count to inform that data was successfully written
                    writer.Advance(bytesRead);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket error occurred: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error in receive loop: {ex.Message}");
                    break;
                }

                // Flush the written data to the PipeReader (notifies the Consumer loop)
                FlushResult result = await writer.FlushAsync(cancellationToken);

                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }

            // Signal completion to the reader
            await writer.CompleteAsync();
        }
    }
}
