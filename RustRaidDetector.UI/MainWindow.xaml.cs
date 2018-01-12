using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReactiveUI;

namespace RustRaidDetector.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow :IViewFor<MainWindowViewModel>
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
                    d(this.OneWayBind(ViewModel, vm => vm.AudioMeter.Items, window => window.AudioMeterControl.ItemsSource));
                });

            this.Events().Loaded.Select(x => Unit.Default).InvokeCommand(ViewModel.UpdateDevices);
        }

        

        public MainWindowViewModel ViewModel
        {
            get => (MainWindowViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = value as MainWindowViewModel;
        }
    }
}
