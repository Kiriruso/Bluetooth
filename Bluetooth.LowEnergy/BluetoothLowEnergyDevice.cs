using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Bluetooth.LowEnergy
{
    public class BluetoothLowEnergyDevice
    {
        #region Public Properties
        
        public BluetoothLEDevice BleDevice { get; protected set; }
        public ulong Address => BleDevice.BluetoothAddress;
        public string Name { get; protected set; }
        public short Signal { get; protected set; }
        public string DeviceId => BleDevice.DeviceId;
        public bool Connected { get; protected set; }
        public bool CanPair => BleDevice.DeviceInformation.Pairing.CanPair;
        public bool Paired => BleDevice.DeviceInformation.Pairing.IsPaired;
        public DateTimeOffset BroadcastTime { get; protected set; }
        
        public static readonly List<string> RequestedProperties = new List<string>()
        {
            "System.ItemNameDisplay",
            "System.Devices.Aep.IsConnected",
            "System.Devices.Aep.SignalStrength"
        };

        #endregion

        #region Constructor

        public BluetoothLowEnergyDevice(
            BluetoothLEDevice bleDevice,
            string name, 
            short signal,
            bool connected,
            DateTimeOffset broadcastTime)
        {
            BleDevice = bleDevice;
            Name = name;
            Signal = signal;
            Connected = connected;
            BroadcastTime = broadcastTime;
        }

        #endregion

        #region Public Service Methods
        
        public static async Task<BluetoothLowEnergyDevice> FromDeviceInformationAsync(DeviceInformation args)
        {
            var device = await BluetoothLEDevice.FromIdAsync(args.Id);
            
            if (device == null)
                return null;

            var (name, connected, rssi) = args.Properties.Parse();

            return new BluetoothLowEnergyDevice(
                bleDevice: device,
                name: name,
                signal: rssi,
                connected: connected,
                broadcastTime: DateTimeOffset.UtcNow
            );
        }

        public static async Task<IReadOnlyCollection<GattDeviceService>> GetGattServicesAsync(BluetoothLowEnergyDevice device)
        {
            GattDeviceServicesResult servicesResult;
            try
            {
                servicesResult = await device.BleDevice.GetGattServicesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new Collection<GattDeviceService>();
            }
            return servicesResult.Status == GattCommunicationStatus.Success 
                ? servicesResult.Services 
                : new Collection<GattDeviceService>();
        }

        public static async Task<IReadOnlyList<GattCharacteristic>> GetCharacteristicsAsync(GattDeviceService service)
        {
            GattCharacteristicsResult characteristicsResult;
            try
            {
                characteristicsResult = await service.GetCharacteristicsAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new List<GattCharacteristic>();
            }
            return characteristicsResult.Status == GattCommunicationStatus.Success
                ? characteristicsResult.Characteristics
                : new List<GattCharacteristic>();
        }

        #endregion

        #region Public Supporting Methods

        public bool TryUpdate(IReadOnlyDictionary<string, object> properties)
        {
            foreach (var property in properties)
            {
                switch (property.Key)
                {
                    case "System.ItemNameDisplay":
                        Name = property.Value != null ? (string)property.Value : "NO NAME";
                        BroadcastTime = DateTimeOffset.UtcNow;
                        return true;
                        
                    case "System.Devices.Aep.IsConnected":
                        if (bool.TryParse(property.Value.ToString(), out var connected))
                        {
                            Connected = connected;
                            BroadcastTime = DateTimeOffset.UtcNow;
                            return true;
                        }
                        return false;
                    
                    case "System.Devices.Aep.SignalStrength":
                        if (short.TryParse(property.Value.ToString(), out var signal))
                        {
                            Signal = signal;
                            BroadcastTime = DateTimeOffset.UtcNow;
                            return true;
                        }
                        return false;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"Device Name:\t{Name}\n" +
                   $"Device Id:\t{DeviceId}\n" +
                   $"Device Address:\t{Address}\n" +
                   $"Is Connected?\t{Connected}\n" +
                   $"Can Pair?\t{CanPair}\n" +
                   $"Paired?\t\t{Paired}\n" +
                   $"Signal:\t\t{Signal}dB\n" +
                   $"Broadcast:\t{BroadcastTime}";
        }
        
        #endregion
    }
}