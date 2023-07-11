using System;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Bluetooth.LowEnergy.Console.Receive
{
    internal static class Receive
    {
        public static void Main(string[] args)
        {
            int heartbeatTimeout = 30;
            short signalStrength = -70;
            BluetoothLowEnergyDeviceWatcher watcher;
            
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

            System.Console.Write("Do you want to search for devices by their name? y/n: ");
            var choice = char.ToUpper(System.Console.ReadKey().KeyChar);
            System.Console.WriteLine();
            if (choice == 'Y')
            {
                System.Console.Write("Enter device name: ");
                var deviceName = System.Console.ReadLine();
                watcher = new BluetoothLowEnergyDeviceWatcher(deviceName: deviceName);
            }
            else
            {
                System.Console.Write("Looking for paired devices? y/n: ");
                choice = char.ToUpper(System.Console.ReadKey().KeyChar);
                System.Console.WriteLine();
                watcher = new BluetoothLowEnergyDeviceWatcher(pairingState: choice == 'Y');
            }

            watcher.HeartbeatTimeout = heartbeatTimeout;
            watcher.SignalStrengthFilter = signalStrength;
            
            watcher.StartedListening += () =>
            {
                System.Console.WriteLine("Listening... Please wait");
            };

            watcher.StoppedListening += () =>
            {
                System.Console.WriteLine("Listening stopped");
            };
            
            watcher.EnumerationCompleted += async () =>
            {
                if (watcher.DevicesFound)
                {
                    System.Console.WriteLine($"Enumeration completed: {watcher.DiscoveredDevices.Count}\n");
                    foreach (var device in watcher.DiscoveredDevices)
                    {
                        if (device == null) 
                            return;
                        
                        System.Console.ForegroundColor = ConsoleColor.White;
                        System.Console.WriteLine(device);
                    
                        var services = await BluetoothLowEnergyDevice.GetGattServicesAsync(device);
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                        if (services.Count == 0)
                        {
                            System.Console.WriteLine("SERVICES NOT FOUND");
                            continue;
                        }
                        System.Console.WriteLine("DEVICE SERVICES: ");
                        foreach (var service in services)
                        {
                            var characteristics = await BluetoothLowEnergyDevice.GetCharacteristicsAsync(service);
                            
                            var (serviceUuid, serviceName, canMaintain) = service.GetInfo();
                            System.Console.ForegroundColor = ConsoleColor.Red;
                            System.Console.WriteLine($"[{serviceUuid}] {serviceName}");
                            System.Console.ForegroundColor = ConsoleColor.White;
                            System.Console.WriteLine($"Can maintain connection: {canMaintain}");
            
                            int i = 1;
                            System.Console.ForegroundColor = ConsoleColor.Yellow;
                            if (characteristics.Count == 0)
                            {
                                System.Console.WriteLine("SERVICE CHARACTERISTICS NOT FOUND");
                                System.Console.WriteLine();
                                continue;
                            }
                            System.Console.WriteLine("SERVICE CHARACTERISTICS:");
                            foreach (var characteristic in characteristics)
                            {
                                var (characteristicUuid, characteristicName, properties, protectionLevel) = characteristic.GetInfo();
                                System.Console.ForegroundColor = ConsoleColor.Green;
                                System.Console.WriteLine($"{i:d2}. [{characteristicUuid}] {characteristicName}");
                                System.Console.ForegroundColor = ConsoleColor.White;
                                System.Console.WriteLine($"    Properties: {properties}");
                                System.Console.WriteLine($"    Protection Level: {protectionLevel}");
                                i += 1;

                                if (characteristic.Uuid != GattCharacteristicUuids.BatteryLevel) 
                                    continue;
                                
                                var readResult = await characteristic.ReadValueAsync();
                                if (readResult.Status == GattCommunicationStatus.Success)
                                {
                                    var reader = DataReader.FromBuffer(readResult.Value);
                                    System.Console.ForegroundColor = ConsoleColor.Blue;
                                    System.Console.WriteLine($"Current Battery LeveL: {reader.ReadByte()}");
                                }

                                var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status != GattCommunicationStatus.Success) 
                                    continue;
                                
                                System.Console.ForegroundColor = ConsoleColor.Cyan;
                                System.Console.WriteLine("Signed up to receive notifications!");
                                characteristic.ValueChanged += (sender, characteristicArgs) =>
                                {
                                    var reader = DataReader.FromBuffer(characteristicArgs.CharacteristicValue);
                                    System.Console.ForegroundColor = ConsoleColor.Blue;
                                    System.Console.WriteLine($"New Battery level: {reader.ReadByte()}");
                                };
                            }
                            System.Console.WriteLine();
                        }
                        System.Console.WriteLine();
                    }
                }
                else
                {
                    System.Console.WriteLine("Devices not found");
                    System.Console.WriteLine();
                }
                System.Console.WriteLine("Press enter to stop listening");
            };

            watcher.StartListening();

            while (watcher.Listening) { } // Waiting for an event [EnumerationCompleted]
            
            System.Console.ReadKey();
            
            watcher.StopListening();
            
            System.Console.WriteLine("Press enter to kill program");
            System.Console.ReadKey();
        }
    }
}