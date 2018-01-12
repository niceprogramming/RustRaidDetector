using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Threading;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
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
        private AudioMeterModel _audioMeter;

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
        public AudioMeterModel AudioMeter
        {
            get { return _audioMeter ?? (_audioMeter = new AudioMeterModel()); }
        }
        public MMDevice SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                AudioMeter.Endpoint = value;
                this.RaiseAndSetIfChanged(ref _selectedDevice,value);
                
            }
        }

        public ReactiveCommand UpdateDevices { get;}
    }
    public sealed class AudioMeterModel : ReactiveObject, IDisposable
    {
        private AudioMeterInformation _audioMeterInformation;

        private MMDevice _endpoint;
        private ObservableCollection<AudioMeterItem> _items;
        private readonly DispatcherTimer _timer;

        private WasapiCapture _dummyCapture;

        public MMDevice Endpoint
        {
            get { return _endpoint; }
            set
            {
                _endpoint = value;
                EnableCaptureEndpoint();

                if (_endpoint != null)
                {
                    _audioMeterInformation = AudioMeterInformation.FromDevice(_endpoint);
                }

                Items = null;
            }
        }

        public ObservableCollection<AudioMeterItem> Items
        {
            get { return _items; }
            set
            {
                this.RaiseAndSetIfChanged(ref _items,value);
            }
        }

        public AudioMeterModel()
        {
           // var timer = Observable.t
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30),
                IsEnabled = true
            };
            _timer.Tick += (s, e) => UpdateItems();
        }

        private void UpdateItems()
        {
            if (_audioMeterInformation == null)
                return;

            CreateItems();

            var values = _audioMeterInformation.GetChannelsPeakValues();
            _items[0].Value = _audioMeterInformation.PeakValue;
            for (int i = 0; i < values.Length; i++)
            {
                _items[i + 1].Value = values[i];
            }
        }

        private void CreateItems()
        {
            if (Items == null)
            {
                Items = new ObservableCollection<AudioMeterItem> { new AudioMeterItem("MasterPeakValue") };
                for (int i = 0; i < _audioMeterInformation.MeteringChannelCount; i++)
                {
                    Items.Add(new AudioMeterItem($"Channel {i + 1}"));
                }
            }
        }

        private void EnableCaptureEndpoint()
        {
            if (_dummyCapture != null)
            {
                _dummyCapture.Dispose();
                _dummyCapture = null;
            }

            if (Endpoint != null && Endpoint.DataFlow == DataFlow.Capture)
            {
                _dummyCapture = new WasapiCapture(true, AudioClientShareMode.Shared, 250) { Device = Endpoint };
                _dummyCapture.Initialize();
                _dummyCapture.Start();
            }
        }

        public class AudioMeterItem : ReactiveObject
        {
            private string _name;
            private float _value;

            public AudioMeterItem(string name)
            {
                Name = name;
            }

            public string Name
            {
                get { return _name; }
                set { this.RaiseAndSetIfChanged(ref _name, value); }
            }

            public float Value
            {
                get { return _value; }
                set
                {
                    this.RaiseAndSetIfChanged(ref _value, value);
                }

            }
        }

        public void Dispose()
        {
            if (_dummyCapture != null)
            {
                _dummyCapture.Dispose();
                _dummyCapture = null;
            }
        }
    }

}