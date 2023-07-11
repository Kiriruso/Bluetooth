using System.Collections.Generic;

namespace Bluetooth.LowEnergy
{
    public static class DeviceInformationPropertiesParser
    {
        public static (string, bool, short) Parse(this IReadOnlyDictionary<string, object> properties)
        {
            int i = 0;
            var rawProperties = new object[3];
            foreach (var property in properties)
                if (BluetoothLowEnergyDevice.RequestedProperties.Contains(property.Key))
                    rawProperties[i++] = property.Value;
            
            var name = rawProperties[0] != null ? (string)rawProperties[0] : "NO NAME";
            var connected = bool.Parse(rawProperties[1].ToString());
            var rssi = short.Parse(rawProperties[2].ToString());

            return (name, connected, rssi);
        }
    }
}