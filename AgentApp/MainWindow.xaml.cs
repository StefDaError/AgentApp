using System.Windows;
using AgentApp.ViewModels;

namespace AgentApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}