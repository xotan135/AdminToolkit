using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using AdminToolkit.Desktop.Models;
using AdminToolkit.Desktop.Services;

namespace AdminToolkit.Desktop;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ComputerResult> _results = [];
    private readonly ComputerStatusService _statusService;
    private readonly DellUpdateScanService _dellScanService;
    private readonly ToolkitConfiguration _configuration;
    private readonly AuditLogService _auditLogService;
    private CancellationTokenSource? _jobCancellation;
    private bool _isInitializing = true;

    public MainWindow()
    {
        _configuration = ToolkitConfiguration.Load();
        _auditLogService = new AuditLogService(_configuration.LogDirectory);
        _statusService = new ComputerStatusService(_configuration.MaximumConcurrency, _configuration.PingTimeoutMilliseconds);
        _dellScanService = new DellUpdateScanService(
            _configuration.MaximumConcurrency,
            _configuration.PowerShellExecutable,
            _configuration.DellCommandUpdate,
            _configuration.DellScanDirectory);
        InitializeComponent();
        ThemeService.Apply(_configuration.DarkMode);
        DarkModeToggle.IsChecked = _configuration.DarkMode;
        ResultsGrid.ItemsSource = _results;
        ActionInput.ItemsSource = ActionCatalog.All;
        ActionInput.SelectedIndex = 0;
        ComputerInput.Focus();
        StatusText.Text = $"Ready · Configuration: {_configuration.FilePath}";
        _isInitializing = false;
    }

    private void DarkModeToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (DarkModeToggle is null) return;
        var darkMode = DarkModeToggle.IsChecked == true;
        ThemeService.Apply(darkMode);
        if (_isInitializing) return;
        _configuration.DarkMode = darkMode;
        try { _configuration.Save(); }
        catch (Exception exception) { StatusText.Text = $"Theme changed, but the preference could not be saved: {exception.Message}"; }
    }

    private void ActionInput_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ActionInput.SelectedItem is not AdminAction action) return;
        RiskText.Text = $"{action.RiskLabel} · {(action.IsAvailable ? "Available" : "Planned migration")}";
        RiskText.Foreground = action.Risk switch
        {
            ActionRisk.High => System.Windows.Media.Brushes.Firebrick,
            ActionRisk.Medium => System.Windows.Media.Brushes.DarkOrange,
            _ => System.Windows.Media.Brushes.SeaGreen
        };
        ActionDescription.Text = action.Description;
        RunButton.IsEnabled = action.IsAvailable && _jobCancellation is null;
    }

    private async void RunButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ActionInput.SelectedItem is not AdminAction action || !action.IsAvailable) return;
        var computers = ComputerNameParser.Parse(ComputerInput.Text);
        if (computers.Count == 0)
        {
            MessageBox.Show(this, "Enter at least one computer name.", "Admin Toolkit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _results.Clear();
        JobProgress.Maximum = computers.Count;
        JobProgress.Value = 0;
        SummaryText.Text = $"0 of {computers.Count} complete";
        _jobCancellation = new CancellationTokenSource();
        SetRunningState(true);
        var progress = new Progress<ComputerResult>(result =>
        {
            _results.Add(result);
            if (ResultsGrid.SelectedItem is null) ResultsGrid.SelectedItem = result;
            JobProgress.Value = _results.Count;
            SummaryText.Text = $"{_results.Count} of {computers.Count} complete";
        });

        try
        {
            StatusText.Text = $"Running {action.Name} on {computers.Count} computer(s)…";
            if (action.Id == "dell-scan")
                await _dellScanService.ScanAsync(computers, progress, _jobCancellation.Token);
            else
                await _statusService.CheckAsync(computers, progress, _jobCancellation.Token);

            var successful = _results.Count(result => result.Status is ResultStatus.Success or ResultStatus.Online);
            StatusText.Text = $"Complete — {successful} successful, {_results.Count - successful} unavailable or failed.";
            SummaryText.Text = $"{_results.Count} results · {successful} successful";
            await _auditLogService.WriteAsync(action, _results, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = $"Cancelled after {_results.Count} of {computers.Count} computers.";
            SummaryText.Text = "Job cancelled";
            await _auditLogService.WriteAsync(action, _results, CancellationToken.None, wasCancelled: true);
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Job failed: {exception.Message}";
            MessageBox.Show(this, exception.Message, "Job failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _jobCancellation.Dispose();
            _jobCancellation = null;
            SetRunningState(false);
        }
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        StatusText.Text = "Cancelling…";
        _jobCancellation?.Cancel();
    }

    private void ResultsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultsGrid.SelectedItem is ComputerResult result)
        {
            DetailsOutput.Text = string.IsNullOrWhiteSpace(result.Details)
                ? "No additional command output is available for this result."
                : result.Details;
        }
    }

    private void SetRunningState(bool isRunning)
    {
        ComputerInput.IsEnabled = !isRunning;
        ActionInput.IsEnabled = !isRunning;
        RunButton.IsEnabled = !isRunning && ActionInput.SelectedItem is AdminAction { IsAvailable: true };
        CancelButton.IsEnabled = isRunning;
    }
}
