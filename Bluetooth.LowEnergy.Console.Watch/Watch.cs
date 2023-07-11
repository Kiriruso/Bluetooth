using System;
using System.Threading;

namespace Bluetooth.LowEnergy.Console.Watch
{
    internal static class Watch
    {
        public static void Main(string[] args)
        {
            int heartbeatTimeout = 30;
            short signalStrength = -70;

            System.Console.Write("Enter the device timeout time (seconds): ");
            while (!int.TryParse(System.Console.ReadLine(), out heartbeatTimeout) & heartbeatTimeout > 0)
            {
                System.Console.WriteLine("Incorrect time");
                System.Console.Write("Enter the device timeout time: ");
            }
            
            System.Console.Write("Enter the strength of the signal [example -50 dB]: ");
            while (!short.TryParse(System.Console.ReadLine(), out signalStrength) & signalStrength < 0)
            {
                System.Console.WriteLine("Signal must be less than 0");
                System.Console.WriteLine("Enter the strength of the signal (dB): ");
            }

            var watcher = new BluetoothLowEnergyDeviceWatcher(pairingState: false)
            {
                HeartbeatTimeout = heartbeatTimeout,  // Default value  30 [in seconds]
                SignalStrengthFilter = signalStrength // Default value -70 [in dB]
            };

            watcher.StartedListening += () =>
            {
                System.Console.WriteLine("Listening... Please wait");
            };

            watcher.StoppedListening += () =>
            {
                System.Console.WriteLine("Listening stopped");
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
                System.Console.WriteLine($"Enumeration completed: {watcher.DiscoveredDevices.Count}");
                Thread.Sleep(5000);
            };

            watcher.StartListening();
            
            while (watcher.Listening) { } // Waiting for an event [EnumerationCompleted]

            if (watcher.DevicesFound)
            {
                System.Console.WriteLine("Discovered devices:");
                foreach (var device in watcher.DiscoveredDevices)
                    System.Console.WriteLine(device + Environment.NewLine);
            }
            else
            {
                System.Console.WriteLine("Devices not found");
            }

            watcher.StopListening();
            
            System.Console.WriteLine("Press enter to kill program");
            System.Console.ReadKey();
        }
    }
}