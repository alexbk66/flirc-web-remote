using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using FlircWrapper;

namespace FlircTestingTool
{
    internal static class Program
    {
        static IntPtr neg_one = new IntPtr(-1);
        static IntPtr zero_ptr = new IntPtr(0);

        static FlircService service = new FlircService();
        static IrProt? lastPacket = null;

        public static void Main(string[] args)
        {
            // dummy program for testing purposes
            // used to build the wrappers around IR & Flirc libraries & communicatation with the actual device


            Trace.Listeners.Add(new ConsoleTraceListener());

            Console.WriteLine("Initializing Flirc...");
            Console.WriteLine("FlircLibrary: " + service.FetchFlircLibraryVersion());
            Console.WriteLine("IrLibrary: " + service.FetchIrLibraryVersion());

            // try open connection to device
            var connected = service.OpenConnection();
            if (connected)
            {
                service.RegisterTransmitter();
                Console.WriteLine("Flirc device opened successfully...");
            }

            // if failed, maybe disconnected, then wait till it is connected
            if (!connected)
            {
                Console.WriteLine("Unable to connect, device not found (most likely), please plug it in...");
                var result = service.WaitForDevice();
                if (!result)
                {
                    Console.WriteLine("Failed connecting, closing program...");
                    return;
                }

                service.OpenConnection();
                service.RegisterTransmitter();
                Console.WriteLine("Flirc device opened successfully...");
            }

            if (!service.RegisterTransmitter())
            {
                Console.WriteLine("Failed to register transmit callback...");
                return;
            }
            Console.WriteLine("Transmitter callback successfully registered...");

            Console.WriteLine("Listening to commands...");
            Console.WriteLine("'send' to resend, 'listen' to record packet for send, 'exit' to close");
            ListenForCommands();
            Console.WriteLine("Closing connection to device...");
            service.CloseConnection();
        }

        static void ListenForCommands()
        {
            var command = "listen";
            while (true)
            {
                //var command = Console.ReadLine() ?? string.Empty;
                if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                if (command.Equals("listen", StringComparison.OrdinalIgnoreCase))
                    RecordPacket();
                else if (command.Equals("send", StringComparison.OrdinalIgnoreCase))
                    TransmitRecordedPacket();
                else
                    Console.WriteLine("Unknown command. Available commands: 'send', 'listen', 'exit'.");
            }
            Console.WriteLine("Stopped listening for commands...");
        }

        static void RecordPacket()
        {
            Console.WriteLine("Listening, press a key on your remote...");
            List<IrProt> protos = service.ListenToPoll(CancellationToken.None);

            // return first "proto/packet" - this is from testing of my devices that has worked...
            // if we need to send more than a single packet for something, then this gotta be changed, along "mapped" object
            if (protos.Any())
            {
                lastPacket = protos.FirstOrDefault();
                //Console.WriteLine("Packet recorded, can now be sent with 'send' command...");
            }
            else
            {
                //Console.WriteLine("No packet recorded, type next command...");
            }
        }

        static void TransmitRecordedPacket()
        {
            if (!lastPacket.HasValue)
            {
                Console.WriteLine("No packet recorded to re-transmit.");
                return;
            }

            var result = service.SendPacket(lastPacket.Value);
            Console.WriteLine(result == zero_ptr ? "Successfully sent packet..." : $"Failed to send packet, status code: {result}");
        }

    }
}