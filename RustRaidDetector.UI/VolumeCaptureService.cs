using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Threading;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using ReactiveUI;

namespace RustRaidDetector.UI
{
    public class VolumeCaptureService : IDisposable
    {
        private readonly TimeSpan _updateFrequency;
       
        private AudioMeterInformation _audioMeterInformation;
        private bool isCapturing;
        private WasapiCapture _dummyCapture;

        private MMDevice _endpoint;
        private List<AudioMeterModel> _items;
        private readonly Subject<List<AudioMeterModel>> _itemsUpdated = new Subject<List<AudioMeterModel>>();

        public IObservable<List<AudioMeterModel>> ItemsUpdated => this._itemsUpdated.AsObservable();

        public VolumeCaptureService(TimeSpan updateFrequency)
        {
            _updateFrequency = updateFrequency;
           
        }

        public MMDevice Endpoint
        {
            get => _endpoint;
            set
            {
                _endpoint = value;
                EnableCaptureEndpoint();

                if (_endpoint != null)
                {
                    _audioMeterInformation = AudioMeterInformation.FromDevice(_endpoint);
                }
                UpdateItems();
                _items = null;
            }
        }

        public void Start()
        {
            isCapturing = true;
            Observable.Interval(_updateFrequency)
                .TakeWhile(x => isCapturing)
                .Subscribe(_ => UpdateItems());

        }
        public void Stop()
        {
            isCapturing = false;
            _items.Clear();
        }
        public void Dispose()
        {
            if (_dummyCapture != null)
            {
                _dummyCapture.Dispose();
                _dummyCapture = null;
            }
        }

        private void UpdateItems()
        {
            if (_audioMeterInformation == null)
            {
                return;
            }

            CreateItems();

            var values = _audioMeterInformation.GetChannelsPeakValues();
            _items[0].Value = _audioMeterInformation.PeakValue *100;
            for (var i = 0; i < values.Length; i++)
            {
                _items[i + 1].Value = values[i] *100;
            }

            try
            {
                _itemsUpdated.OnNext(_items);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
               _itemsUpdated.OnError(e);
            }
         
        }

        private void CreateItems()
        {
            if (_items != null)
            {
                return;
            }

            _items = new List<AudioMeterModel> {new AudioMeterModel("MasterPeakValue")};
            for (var i = 0; i < _audioMeterInformation.MeteringChannelCount; i++)
            {
                _items.Add(new AudioMeterModel($"Channel {i + 1}"));
            }
        }

        private void EnableCaptureEndpoint()
        {
            if (_dummyCapture != null)
            {
                _dummyCapture.Dispose();
                _dummyCapture = null;
            }

            if (Endpoint == null || Endpoint.DataFlow != DataFlow.Capture)
            {
                return;
            }

            _dummyCapture = new WasapiCapture(true, AudioClientShareMode.Shared, 250) {Device = Endpoint};
            _dummyCapture.Initialize();
            _dummyCapture.Start();
        }
    }
}