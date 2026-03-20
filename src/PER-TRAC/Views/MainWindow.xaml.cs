using System;
using System.Windows;
using PerTrac.ViewModels;

namespace PerTrac.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Dispose();
        base.OnClosed(e);
    }
}
