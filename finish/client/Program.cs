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
/* Step 2 */
using Eclypses.MTE;

namespace Client
{
    class Program
    {
        /* Step 3 */
        private static MteBase _mteBase = new MteBase();

        //---------------------------------------------------
        // Comment out to use MKE or MTE FLEN instead of MTE Core
        //---------------------------------------------------
        private static MteDec _decoder = new MteDec();
        private static MteEnc _encoder = new MteEnc();

        //---------------------------------------------------
        // Uncomment to use MKE instead of MTE Core
        //---------------------------------------------------
        // private static MteMkeDec _decoder = new MteMkeDec();
        // private static MteMkeEnc _encoder = new MteMkeEnc();

        //---------------------------------------------------
        // Uncomment to use MTE FLEN instead of MTE Core
        //---------------------------------------------------
        // private const int _fixedLength = 8;
        // private static MteFlenEnc _encoder = new MteFlenEnc(_fixedLength);
        // private static MteDec _decoder = new MteDec();

        private static MteStatus _encoderStatus = MteStatus.mte_status_success;
        private static MteStatus _decoderStatus = MteStatus.mte_status_success;

        /* Step 4 */
        // Set default entropy, nonce and identifier
        // Providing Entropy in this fashion is insecure. This is for demonstration purposes only and should never be done in practice.
        // If this is a trial version of the MTE, entropy must be blank
        private static string _encoderEntropy = "";
        private static string _decoderEntropy = "";
        // OPTIONAL!!! adding 1 to Encoder nonce so return value changes -- same nonce can be used for Encoder and Decoder
        // on server side values will be switched so they match up Encoder to Decoder and vice versa
        private static ulong _encoderNonce = 1;
        private static ulong _decoderNonce = 0;
        private static string _identifier = "demo";
        private static string _licenseCompany = "Eclypses Inc.";
        private static string _licenseKey = "Eclypses123";
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            // This tutorial uses HTTP for communication.
            // It should be noted that MTE can be used with any type of communication. (HTTP is not required!)

            // Display what version of MTE we are using
            string mteVersion = _mteBase.GetVersion();
            Console.WriteLine($"Using MTE Version {mteVersion}");

            /* Step 5 */
            // Check mte license
            // Initialize MTE license. If a license code is not required (e.g., trial mode), this can be skipped.
            if (!_mteBase.InitLicense(_licenseCompany, _licenseKey))
            {
                _encoderStatus = MteStatus.mte_status_license_error;
                throw new Exception($"License error ({_mteBase.GetStatusName(_encoderStatus)}): {_mteBase.GetStatusDescription(_encoderStatus)}.  Press any key to end.");
            }

            /* Step 6 */
            // Instantiate the Encoder
            InstantiateEncoder();

            /* Step 6 CONTINUED... */
            // Instantiate the Decoder
            InstantiateDecoder();

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

                /* Step 7 */
                // Encode returning text and ensuring successful
                string encodedMessage = _encoder.EncodeB64(message, out _encoderStatus);

                if (_encoderStatus != MteStatus.mte_status_success)
                {
                    Console.WriteLine($"Error encoding: Status: {_mteBase.GetStatusName(_encoderStatus)} / {_mteBase.GetStatusDescription(_encoderStatus)}");
                    break;
                }

                byte[] messageResponse = await SendMessage($"http://{ip}:{port}/echo", encodedMessage);

                // For demonstration purposes only to show packets
                Console.WriteLine($"\nMTE packet Sent: {encodedMessage}");

                /* Step 7 CONTINUED... */
                // Decode received bytes and check to ensure successful result
                string decodedMessage = _decoder.DecodeStr(messageResponse, out _decoderStatus);

                if (_decoderStatus != MteStatus.mte_status_success)
                {
                    Console.WriteLine($"Error decoding: Status: {_mteBase.GetStatusName(_decoderStatus)} / {_mteBase.GetStatusDescription(_decoderStatus)}");
                    break;
                }

                // For demonstration purposes only to show packets
                Console.WriteLine($"\nReceived MTE packet: {Convert.ToBase64String(messageResponse)}");
                Console.WriteLine($"\nDecoded data: {decodedMessage}");

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

        private static void InstantiateEncoder()
        {
            try
            {
                // Check how long entropy we need and set default
                int entropyMinBytes = _mteBase.GetDrbgsEntropyMinBytes(_encoder.GetDrbg());
                _encoderEntropy = (entropyMinBytes > 0) ? new String('0', entropyMinBytes) : _encoderEntropy;

                _encoder.SetEntropy(Encoding.UTF8.GetBytes(_encoderEntropy));
                _encoder.SetNonce(_encoderNonce);

                _encoderStatus = _encoder.Instantiate(_identifier);

                if (_encoderStatus != MteStatus.mte_status_success)
                {
                    throw new ApplicationException($"Failed to initialize the MTE Encoder engine. Status: {_mteBase.GetStatusName(_encoderStatus)} / {_mteBase.GetStatusDescription(_encoderStatus)}");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to initialize the MTE Encoder engine. Status: {_mteBase.GetStatusName(_encoderStatus)} / {_mteBase.GetStatusDescription(_encoderStatus)}\n\nError: {ex}");
            }
        }

        private static void InstantiateDecoder()
        {
            try
            {
                // Check how long entropy we need and set default
                int entropyMinBytes = _mteBase.GetDrbgsEntropyMinBytes(_decoder.GetDrbg());
                _decoderEntropy = (entropyMinBytes > 0) ? new String('0', entropyMinBytes) : _decoderEntropy;

                _decoder.SetEntropy(Encoding.UTF8.GetBytes(_decoderEntropy));
                _decoder.SetNonce(_decoderNonce);

                _decoderStatus = _decoder.Instantiate(_identifier);

                if (_decoderStatus != MteStatus.mte_status_success)
                {
                    throw new ApplicationException($"Failed to initialize the MTE Decoder engine.  Status: {_mteBase.GetStatusName(_decoderStatus)} / {_mteBase.GetStatusDescription(_decoderStatus)}");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to initialize the MTE Decoder engine.  Status: {_mteBase.GetStatusName(_decoderStatus)} / {_mteBase.GetStatusDescription(_decoderStatus)}\n\nError: {ex}");
            }
        }
    }
}