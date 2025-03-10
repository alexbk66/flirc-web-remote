using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
            uint scancode = 0; // 384415648;
            RcProto protocol = RcProto.RC_PROTO_NEC;
            if (args.Length >= 2)
            {
                uint.TryParse(args[0], out scancode);
                Enum.TryParse(args[1], out protocol);
            }

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


            if (scancode != 0)
            {
                lastPacket = new IrProt()
                {
                    scancode = scancode,
                    protocol = protocol,
                };

                TransmitRecordedPacket();
            }
            else
            {
                Console.WriteLine("Listening to commands...");
                ListenForCommands();
            }


            Console.WriteLine("Closing connection to device...");
            service.CloseConnection();
        }

        const string command_listen = "l";
        const string command_send = "s";
        const string command_quit = "q";

        static void ListenForCommands()
        {
            string usage = $"Available commands: '{command_listen}', '{command_send}', '{command_quit}'.";
            Console.WriteLine(usage);

            bool running = true;
            while (running)
            {
                var command = Console.ReadLine() ?? string.Empty;

                switch (command)
                {
                    case command_listen:
                        RecordPacket();
                        break;

                    case command_send:
                        TransmitRecordedPacket();
                        break;

                    case command_quit:
                        Console.WriteLine("Stopped listening for commands...");
                        running = false;
                        break;

                    default:
                        Console.WriteLine($"Unknown command. {usage}");
                        break;
                }
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
                if (lastPacket.HasValue)
                {
                    IrProt p = lastPacket.Value;
                    int size1 = Marshal.SizeOf<IrProt>();
                    int size2 = Unsafe.SizeOf<IrProt>();
                }
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