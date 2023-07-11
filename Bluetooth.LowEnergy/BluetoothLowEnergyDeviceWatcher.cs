using System;
using System.Linq;
using System.Collections.Generic;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace Bluetooth.LowEnergy
{
    public class BluetoothLowEnergyDeviceWatcher
    {
        #region Private Members
        
        private readonly DeviceWatcher _watcher;
        private readonly object _threadLock = new object();
        private readonly Dictionary<string, BluetoothLowEnergyDevice> _discoveredDevices = new Dictionary<string, BluetoothLowEnergyDevice>();

        #endregion
        
        #region Public Properties

        public bool Listening => _watcher.Status == DeviceWatcherStatus.Started;
        public bool Updated => _watcher.Status == DeviceWatcherStatus.EnumerationCompleted;
        public bool Stopped => _watcher.Status == DeviceWatcherStatus.Stopped;
        public bool DevicesFound => DiscoveredDevices.Count > 0;
        public IReadOnlyCollection<BluetoothLowEnergyDevice> DiscoveredDevices
        {
            get
            {
                lock (_threadLock)
                {
                    return _discoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }
        public int HeartbeatTimeout { get; set; } = 30;        // seconds
        public short SignalStrengthFilter { get; set; } = -70; // dB

        #endregion

        #region Public Events

        public event Action StartedListening = () => { };
        public event Action StoppedListening = () => { };
        public event Action EnumerationCompleted = () => { };
        public event Action<BluetoothLowEnergyDevice> NewDeviceDiscovered = (device) => { };
        public event Action<BluetoothLowEnergyDevice> DeviceUpdated = (device) => { };
        public event Action<BluetoothLowEnergyDevice> DeviceTimeout = (device) => { };

        #endregion
        
        #region Constructors
        
        public BluetoothLowEnergyDeviceWatcher(BluetoothLEAppearance appearance)
        {
            _watcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromAppearance(appearance),
                BluetoothLowEnergyDevice.RequestedProperties,
                DeviceInformationKind.AssociationEndpoint
            );
        }

        public BluetoothLowEnergyDeviceWatcher(ulong bluetoothAddress)
        {
            _watcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(bluetoothAddress),
                BluetoothLowEnergyDevice.RequestedProperties,
                DeviceInformationKind.AssociationEndpoint
            );
        }
        
        public BluetoothLowEnergyDeviceWatcher(string deviceName)
        {
            _watcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromDeviceName(deviceName),
                BluetoothLowEnergyDevice.RequestedProperties,
                DeviceInformationKind.AssociationEndpoint
            );
        }

        public BluetoothLowEnergyDeviceWatcher(bool pairingState)
        {
            _watcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromPairingState(pairingState),
                BluetoothLowEnergyDevice.RequestedProperties,
                DeviceInformationKind.AssociationEndpoint
            );
        }

        public BluetoothLowEnergyDeviceWatcher(BluetoothConnectionStatus connectionStatus)
        {
            _watcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(connectionStatus),
                BluetoothLowEnergyDevice.RequestedProperties,
                DeviceInformationKind.AssociationEndpoint
            );
        }

        #endregion

        #region Public Start and Stop listening Methods

        public void StartListening()
        {
            if (Listening)
                return;

            SubscribeWatcher();
            _watcher.Start();
            StartedListening();
        }
        
        public void StopListening()
        {
            if (Stopped)
                return;

            UnsubscribeWatcher();
            _watcher.Stop();
            while (_watcher.Status != DeviceWatcherStatus.Stopped) { }
            StoppedListening();
            
            lock (_discoveredDevices)
            {
                _discoveredDevices.Clear();
            }
        }

        #endregion

        #region Private Subscribe Methods
        
        private void SubscribeWatcher()
        {
            _watcher.Added += OnAdvertisementReceivedAsync;
            _watcher.Updated += WatcherUpdated;
            _watcher.Removed += (sender, args) => { };
            _watcher.Stopped += (sender, args) => { };
            _watcher.EnumerationCompleted += WatcherEnumerationCompleted;
        }

        private void UnsubscribeWatcher()
        {
            _watcher.Added -= OnAdvertisementReceivedAsync;
            _watcher.Updated -= WatcherUpdated;
            _watcher.EnumerationCompleted -= WatcherEnumerationCompleted;
        }
        
        #endregion
        
        #region Private Service Methods
        
        private async void OnAdvertisementReceivedAsync(DeviceWatcher sender, DeviceInformation args)
        {
            CleanupTimeouts();
            if (IsUnfiltered(args.Properties))
                return;

            BluetoothLowEnergyDevice newDevice;
            try
            {
                newDevice = await BluetoothLowEnergyDevice
                    .FromDeviceInformationAsync(args)
                    .ConfigureAwait(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            if (newDevice == null)
                return;

            lock (_discoveredDevices)
            {
                if (_discoveredDevices.ContainsKey(newDevice.DeviceId))
                {
                    _discoveredDevices.TryUpdate(args.Id, args.Properties);
                    DeviceUpdated(_discoveredDevices[newDevice.DeviceId]);
                }
                else
                {
                    _discoveredDevices[newDevice.DeviceId] = newDevice;
                    NewDeviceDiscovered(newDevice);
                }
            }
        }

        private void CleanupTimeouts()
        {
            lock (_threadLock)
            {
                _discoveredDevices.Values
                    .Where(device =>
                    {
                        var threshold = DateTimeOffset.UtcNow - device.BroadcastTime;
                        return threshold > TimeSpan.FromSeconds(HeartbeatTimeout);
                    })
                    .ToList()
                    .ForEach(device => 
                    {
                        _discoveredDevices.Remove(device.DeviceId); 
                        DeviceTimeout(device);
                    });
            }
        }
        
        private bool IsUnfiltered(IReadOnlyDictionary<string, object> properties)
        {
            if (properties.TryGetValue("System.Devices.Aep.SignalStrength", out var rawSignal))
                if (short.Parse(rawSignal.ToString()) < SignalStrengthFilter)
                    return true;
            return false;
        }
        
        private void WatcherUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            CleanupTimeouts();
            if (IsUnfiltered(args.Properties))
                return;

            lock (_discoveredDevices)
            {
                if (_discoveredDevices.ContainsKey(args.Id))
                    if (_discoveredDevices.TryUpdate(args.Id, args.Properties)) 
                        DeviceUpdated(_discoveredDevices[args.Id]);
            }
        }

        private void WatcherEnumerationCompleted(DeviceWatcher sender, object args)
        {
            CleanupTimeouts();
            EnumerationCompleted();
        }

        #endregion
    }
}