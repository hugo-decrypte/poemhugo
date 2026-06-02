using PoemClientWPF.Tools;
using System.Windows;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace PoemClientWPF.View
{
    public partial class OpenView : Window
    {
        private readonly UserControlView _view;
        private MainWindow _main;

        public OpenView(string viewFile, MainWindow main)
        {
            InitializeComponent();
            _main = main;
            
            gridOpen.Children.Add((_view = new UserControlView(null, main)));

            _view.InitializeView();
            _view.SizeChanged += (object sender, SizeChangedEventArgs _e) => {
                ((UserControlView)sender).SetViewBestFit();
            };
            _ = _view.LoadView(viewFile);
            InitializeDataContext();
        }

        private void InitializeDataContext()
        {
            DataContext = new
            {
                WindowName = MainWindow.config.GetKeyValue("ViewWindow")
            };
        }
    }
}
