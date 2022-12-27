using AtaraxiaAI.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace AtaraxiaAI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainWindowViewModel? viewModel = App.Current?.Services?.GetService<MainWindowViewModel>();
            DataContext = viewModel;

            if (viewModel != null)
            {
                // Set default UI background. In most scenarios the vision engine will take over.
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AtaraxiaAI.Base.Assets.sample-background.jpg");
                viewModel.VisionFrame = new Avalonia.Media.Imaging.Bitmap(stream);
            }
        }
    }
}
