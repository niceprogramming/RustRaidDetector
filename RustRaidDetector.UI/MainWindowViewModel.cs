using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using CSCore.CoreAudioAPI;
using CSCore.Streams.Effects;
using ReactiveUI;

namespace RustRaidDetector.UI
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly MMNotificationClient _notificationClient;
        private ObservableCollection<MMDevice> _devices;
        private VolumeCaptureService _meter;
        private MMDevice _selectedDevice;

        private List<float> test = new List<float>();

        private float _peakVolume;

        private double _peakOffset;
        //    private ObservableAsPropertyHelper<ObservableCollection<AudioMeterModel>> _audioMeters;

        public MainWindowViewModel()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _notificationClient = new MMNotificationClient(_deviceEnumerator);

            CaptureService.ItemsUpdated.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
            {
             
                if (AudioMeters.Count == 0)
                {
                    foreach (var audioMeterModel in x)
                    {
                        AudioMeters.Add(audioMeterModel);
                    }

                    return;
                }

                if (PeakVolume > 0)
                {
                    if (x[0].Value > PeakVolume + PeakOffset)
                    {
                        Console.WriteLine("ALERT");
                    }
                }
                 test.Add(x[0].Value);
                for (var i = 0; i < x.Count; i++)
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

            StartVolumeCapture =
                ReactiveCommand.Create(() => { CaptureService.Start(); }, outputScheduler: RxApp.MainThreadScheduler);
            StopVolumeCapture =
                ReactiveCommand.Create(() => { CaptureService.Stop(); }, outputScheduler: RxApp.MainThreadScheduler);
            PeakVolumeCapture =
                ReactiveCommand.CreateFromObservable<Unit, Unit>(_ =>
                {
                    CaptureService.Start();
                    test.Clear();
                    return Observable.Return(Unit.Default)
                        .Delay(TimeSpan.FromSeconds(10))
                        .Do(x =>CaptureService.Stop());
                });
            PeakVolumeCapture.Subscribe(x => PeakVolume = test.Max());
            PeakVolumeCapture.ThrownExceptions.Subscribe(Console.WriteLine);
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

        public VolumeCaptureService CaptureService =>
            _meter ?? (_meter = new VolumeCaptureService(TimeSpan.FromMilliseconds(10)));

        public MMDevice SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                CaptureService.Endpoint = value;
                this.RaiseAndSetIfChanged(ref _selectedDevice, value);
            }
        }

        public double PeakOffset
        {
            get { return _peakOffset; }
            set {this.RaiseAndSetIfChanged(ref _peakOffset, value); }
        }

        public float PeakVolume
        {
            get => _peakVolume;
            set => this.RaiseAndSetIfChanged(ref _peakVolume,value);
        }

        public ObservableCollection<AudioMeterModel> AudioMeters { get; set; } =
            new ObservableCollection<AudioMeterModel>();

        public ReactiveCommand UpdateDevices { get; }
        public ReactiveCommand StartVolumeCapture { get; }
        public ReactiveCommand StopVolumeCapture { get; }
        public ReactiveCommand<Unit, Unit> PeakVolumeCapture { get; }
    }
}