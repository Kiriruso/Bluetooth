using System;
using System.Threading;

namespace Bluetooth.LowEnergy.Console.Watch
{
    internal static class Watch
    {
        public static void Main(string[] args)
        {
            var watcher = new BluetoothLowEnergyDeviceWatcher(pairingState: false)
            {
                HeartbeatTimeout = 15,     // Default value  30 [in seconds]
                SignalStrengthFilter = -70 // Default value -70 [in dB]
            };

            watcher.StartedListening += () =>
            {
                System.Console.WriteLine("Listening...");
            };

            watcher.StoppedListening += () =>
            {
                System.Console.WriteLine("Stopped");
            };
            
            watcher.NewDeviceDiscovered += device =>
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine(device);
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.White;
            }; 
            
            watcher.DeviceTimeout += device =>
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine(device);
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.White;
            }; 
            
            watcher.DeviceUpdated += device =>
            {
                System.Console.ForegroundColor = ConsoleColor.Blue;
                System.Console.WriteLine(device);
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.White;
            };
            
            watcher.EnumerationCompleted += () =>
            {
                System.Console.WriteLine("Press enter to stop listening");
                Thread.Sleep(5000);
            };

            System.Console.WriteLine("Press enter to start listening");
            System.Console.ReadKey();

            watcher.StartListening();
            
            while (watcher.Listening) { } // Waiting for an event [EnumerationCompleted]
            
            System.Console.ReadKey();

            System.Console.WriteLine("Discovered devices:");
            foreach (var device in watcher.DiscoveredDevices)
                System.Console.WriteLine(device + Environment.NewLine);

            watcher.StopListening();
            
            System.Console.WriteLine("Press enter to kill program");
            System.Console.ReadKey();
        }
    }
}