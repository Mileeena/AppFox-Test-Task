using Avalonia.Controls;
using Splat;
using AppFoxTT.Models;
using Avalonia.Markup.Xaml;

namespace AppFoxTT.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Locator.CurrentMutable.RegisterConstant(new ScreensHelper(Screens.All));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
