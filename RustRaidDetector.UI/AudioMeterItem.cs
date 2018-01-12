using ReactiveUI;

namespace RustRaidDetector.UI
{
    public class AudioMeterModel : ReactiveObject
    {
        private string _name;
        private float _value;

        public AudioMeterModel(string name)
        {
            Name = name;
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public float Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }
    }
}