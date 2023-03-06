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
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    class Program
    {
        // Here is where you would want to define your default settings for the MTE
        public static HttpListener listener;

        public static void Main(string[] args)
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

            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add($"http://{ip}:{port}/");
            listener.Start();
            Console.WriteLine($"Listening for connections on http://{ip}:{port}/");

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            Console.WriteLine("Closing server, press ENTER to close");
            Console.ReadLine();
            listener.Close();
        }

        public static async Task HandleIncomingConnections()
        {
            // While user hasn't echoed 'quit', keep on handling requests
            while (true)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest request = ctx.Request;
                HttpListenerResponse response = ctx.Response;

                // Handle echo request and response
                if ((request.HttpMethod == "POST") && (request.Url.AbsolutePath == "/echo"))
                {
                    response.AddHeader("Access-Control-Allow-Origin", "*");
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");

                    // Read request and echo it to console
                    StreamReader reader = new StreamReader(request.InputStream);
                    string message = await reader.ReadToEndAsync();

                    // MTE Decoding the received message would go here

                    Console.WriteLine($"Received packet: {message}");

                    // MTE Encoding the text would go here instead of using the C# stdlib to encode to bytes
                    byte[] encodedMessage = Encoding.UTF8.GetBytes(message);

                    // Send the encoded message over HTTP
                    await response.OutputStream.WriteAsync(encodedMessage, 0, encodedMessage.Length);

                    if (message.Equals("quit", StringComparison.CurrentCultureIgnoreCase))
                    {
                        break;
                    }
                }
                else
                {
                    response.StatusCode = 404;
                    response.StatusDescription = "Requested URI NOT found!";
                }

                response.Close();
            }
        }
    }
}