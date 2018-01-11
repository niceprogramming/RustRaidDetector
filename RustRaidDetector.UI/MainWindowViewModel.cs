using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using CSCore.CoreAudioAPI;
using CSCore.Streams.Effects;
using ReactiveUI;

namespace RustRaidDetector.UI
{
    public class MainWindowViewModel : ReactiveObject
    {
        private ObservableCollection<MMDevice> _devices;
        private MMDevice _selectedDevice;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private MMNotificationClient _notificationClient;

        public MainWindowViewModel()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _notificationClient = new MMNotificationClient(_deviceEnumerator);
       

            UpdateDevices = ReactiveCommand.Create(() =>
            {
                Devices.Clear();
                foreach (var device in _deviceEnumerator.EnumAudioEndpoints(DataFlow.All, DeviceState.Active))
                {
                    Devices.Add(device);
                }
            });

            
            Observable.Merge(
                    Observable.FromEventPattern<DeviceNotificationEventArgs>(
                        x => _notificationClient.DeviceAdded += x,
                        x => _notificationClient.DeviceAdded -= x),
                    Observable.FromEventPattern<DeviceNotificationEventArgs>(
                        y => _notificationClient.DeviceRemoved += y,
                        y => _notificationClient.DeviceRemoved -= y))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => Unit.Default)
                .InvokeCommand(UpdateDevices);

            Observable.FromEventPattern<DevicePropertyChangedEventArgs>(
                    z => _notificationClient.DevicePropertyChanged += z,
                    z => _notificationClient.DevicePropertyChanged -= z)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => Unit.Default)
                .InvokeCommand(UpdateDevices);

        }

       
   

        public ObservableCollection<MMDevice> Devices => _devices ?? (_devices = new ObservableCollection<MMDevice>());

        public MMDevice SelectedDevice
        {
            get { return _selectedDevice; }
            set { this.RaiseAndSetIfChanged(ref _selectedDevice,value); }
        }

        public ReactiveCommand UpdateDevices { get;}
    }

  
}