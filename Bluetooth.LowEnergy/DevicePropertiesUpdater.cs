using System.Collections.Generic;

namespace Bluetooth.LowEnergy
{
    public static class DevicePropertiesUpdater
    {
        public static bool TryUpdate(
            this IReadOnlyDictionary<string, BluetoothLowEnergyDevice> devices,
            string deviceId, 
            IReadOnlyDictionary<string, object> updatedProperties
        )
        {
            return devices[deviceId].TryUpdate(updatedProperties);
        }
    }
}