using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace AtaraxiaAI.Presentation.Desktop.Views;

public partial class MainView : UserControl
{
    private readonly DispatcherTimer _resizeTimer = new() { Interval = TimeSpan.FromMilliseconds(150) };

    public MainView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        _resizeTimer.Tick += OnResizeSettled;
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        VectorBackground.IsVisible = false;
        _resizeTimer.Stop();
        _resizeTimer.Start();
    }

    private void OnResizeSettled(object? sender, EventArgs e)
    {
        _resizeTimer.Stop();
        VectorBackground.IsVisible = true;
    }
}
