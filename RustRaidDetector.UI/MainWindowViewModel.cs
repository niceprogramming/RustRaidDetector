using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using CSCore.CoreAudioAPI;
using ReactiveUI;

namespace RustRaidDetector.UI
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private VolumeCaptureService _meter;
        private ObservableCollection<MMDevice> _devices;
        private readonly MMNotificationClient _notificationClient;
        private MMDevice _selectedDevice;

        private List<float> test = new List<float>();
    //    private ObservableAsPropertyHelper<ObservableCollection<AudioMeterModel>> _audioMeters;

        public MainWindowViewModel()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _notificationClient = new MMNotificationClient(_deviceEnumerator);
           
            Meter.ItemsUpdated.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
            {
                if ( AudioMeters.Count == 0)
                {
                    foreach (var audioMeterModel in x)
                    {
                        AudioMeters.Add(audioMeterModel);
                    }
                    return;
                }
               test.Add(x[0].Value);
                for (int i = 0; i < x.Count; i++)
                {
                    AudioMeters[i] = x[i];
                }
                

            });
            
           
      
            UpdateDevices = ReactiveCommand.Create(() =>
            {
                Devices.Clear();
                foreach (var device in _deviceEnumerator.EnumAudioEndpoints(DataFlow.All, DeviceState.Active))
                {
                    Devices.Add(device);
                }
            });

            StartVolumeCapture = ReactiveCommand.Create(() =>
            {
                Meter.Start();
            },outputScheduler: RxApp.MainThreadScheduler);
            StopVolumeCapture = ReactiveCommand.Create(() =>
            {
                Meter.Stop();
              
            },outputScheduler: RxApp.MainThreadScheduler);

            Observable.FromEventPattern<DeviceNotificationEventArgs>(
                    x => _notificationClient.DeviceAdded += x,
                    x => _notificationClient.DeviceAdded -= x).Merge(
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

        public VolumeCaptureService Meter => _meter ?? (_meter = new VolumeCaptureService(TimeSpan.FromMilliseconds(10)));
       
        public MMDevice SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                Meter.Endpoint = value;
                this.RaiseAndSetIfChanged(ref _selectedDevice, value);
            }
        }

        public ObservableCollection<AudioMeterModel> AudioMeters { get; set; } = new ObservableCollection<AudioMeterModel>();
        public ReactiveCommand UpdateDevices { get; }
        public ReactiveCommand StartVolumeCapture { get; }
        public ReactiveCommand StopVolumeCapture { get; }
    }
}