using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;

namespace RustRaidDetector.UI
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IViewFor<MainWindowViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(MainWindowViewModel), typeof(MainWindow),
                new PropertyMetadata(null));

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.Devices, window => window.DevicesComboBox.ItemsSource));
                d(this.Bind(ViewModel, vm => vm.SelectedDevice, window => window.DevicesComboBox.SelectedItem));
                d(this.OneWayBind(ViewModel, vm => vm.AudioMeters,
                    window => window.AudioMeterControl.ItemsSource));
                d(this.BindCommand(ViewModel, vm => vm.StartVolumeCapture, window => window.StartButton));
                d(this.BindCommand(ViewModel, vm => vm.StopVolumeCapture, window => window.StopButton));
            });

            this.Events().Loaded.Select(x => Unit.Default).InvokeCommand(ViewModel.UpdateDevices);
        }
        

        public MainWindowViewModel ViewModel
        {
            get => (MainWindowViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = value as MainWindowViewModel;
        }
    }
}