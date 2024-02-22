/*
THIS SOFTWARE MAY NOT BE USED FOR PRODUCTION. Otherwise,
The MIT License (MIT)

Copyright (c) Eclypses, Inc.

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        // Here is where you would want to define your default settings for the MTE
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            // This tutorial uses HTTP for communication.
            // It should be noted that the MTE can be used with any type of communication. (HTTP is not required!)

            // Here is where you would want to gather settings for the MTE and check MTE license

            // Set default IP - but also prompt for IP in case user cannot use our default
            string ip = "localhost";

            Console.Write($"Please enter IP to use, default IP is {ip}: ");
            string newIp = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newIp))
            {
                ip = newIp;
            }

            // Set default port - but also prompt for port in case user cannot use our default
            int port = 27015;

            Console.Write($"Please enter port to use, default port is {port}: ");
            string newPort = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newPort))
            {
                while (!int.TryParse(newPort, out port))
                {
                    Console.WriteLine($"{newPort} is not a valid integer, please try again.");
                    newPort = Console.ReadLine();
                }
            }

            // While user hasn't echoed 'quit', keep on handling requests
            while (true)
            {
                Console.Write("\nPlease enter text to send (to end please type 'quit'): ");
                string message = Console.ReadLine();

                // MTE Encoding the message would go here

                byte[] messageResponse = await SendMessage($"http://{ip}:{port}/echo", message);

                // MTE Decoding the byte array would go here

                Console.WriteLine($"Received packet: {Encoding.UTF8.GetString(messageResponse)}");

                if (message.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                {
                    break;
                }
            }

            Console.WriteLine("Client closed, please hit ENTER to end this...");
            Console.ReadLine();
        }

        private static async Task<byte[]> SendMessage(string uri, string message)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));

            var stringContent = new StringContent(message, Encoding.UTF8, "text/plain");
            var response = await client.PostAsync(uri, stringContent);
            var responseMessage = await response.Content.ReadAsStreamAsync();
            byte[] buffer = new byte[responseMessage.Length];
            var result = responseMessage.ReadAsync(buffer, CancellationToken.None);

            return buffer;
        }
    }
}