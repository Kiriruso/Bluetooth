using System;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Bluetooth.LowEnergy.Console.Receive
{
    internal static class Receive
    {
        public static void Main(string[] args)
        {
            var watcher = new BluetoothLowEnergyDeviceWatcher(deviceName: "Mi Smart Band 6");

            watcher.StartedListening += () =>
            {
                System.Console.WriteLine("Listening...");
            };

            watcher.StoppedListening += () =>
            {
                System.Console.WriteLine("Stopped");
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

            System.Console.WriteLine("Press enter to start listening");
            System.Console.ReadKey();
            
            watcher.StartListening();

            while (watcher.Listening) { } // Waiting for an event [EnumerationCompleted]
            
            System.Console.ReadKey();
            
            watcher.StopListening();
            
            System.Console.WriteLine("Press enter to kill program");
            System.Console.ReadKey();
        }
    }
}