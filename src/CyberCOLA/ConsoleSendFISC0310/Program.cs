using Com.Cybersoft4u.CyberUtils.Utils;
using System.Net;
using System.Net.Sockets;

class Server
{
    static async Task Main(string[] args)
    {
        var listener = new TcpListener(IPAddress.Any, 8100);
        listener.Start();

        Console.WriteLine("Server started, listening on port 8100");

        do
        {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected");

            // Handle the client connection
            await HandleClientAsync(client);

            Console.WriteLine("Client disconnected");

            Console.WriteLine("Press ESC to stop Server or continue ");
            while (!Console.KeyAvailable)
            {
                // Do something
            }
        } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
    }

    static async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();

            // Set a timeout of 5 seconds for the communication
            stream.ReadTimeout = 2000;
            stream.WriteTimeout = 2000;

            var fileName = @"E:\FirstBank\Development\CyberCOLA\src\CyberCOLA\WpfStressSimulator\Data\Fisc\fisc0310_asc.txt";
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File[{fileName}] Not Found");
                return;
            }
            var sendBufferData = File.ReadAllBytes(fileName);
            do
            {
                try
                {
                    //send message to client
                    stream.WriteTimeout = 2000;
                    await stream.WriteAsync(sendBufferData, 0, sendBufferData.Length);
                    Console.WriteLine($"Sent message to client: {BitConverter.ToString(sendBufferData)}");

                    //await Task.Delay(1000); // Wait 1 second before sending the next message

                    // Receive a message from the client
                    stream.ReadTimeout = 1000;
                    byte[] receiveBuffer = new byte[2048];
                    var readTask = stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                    var completedTask = await Task.WhenAny(readTask, Task.Delay(1000));
                    if (completedTask != readTask)
                    {
                        // Timeout elapsed before read operation completed
                        throw new System.TimeoutException("Receive operation timed out");
                    }
                    var bytesRead = await readTask;
                    Console.WriteLine($"Received message from client: {BitConverter.ToString(CommonUtils.GetSubBytes(receiveBuffer, 0, bytesRead))}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                Console.WriteLine("Press ESC to stop Process or continue process ");
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }
}
