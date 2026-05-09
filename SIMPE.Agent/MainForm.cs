using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SIMPE.Agent;

public class MainForm : Form
{
    private readonly WebView2 _webView;
    private readonly CancellationTokenSource _cts;

    public MainForm(CancellationTokenSource cts)
    {
        _cts = cts;
        Text = "SIMPE Agent";
        Size = new System.Drawing.Size(1366, 768);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

        // Use a user-writable folder for WebView2 data (required when installed in Program Files)
        string webViewDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SIMPE Agent",
            "WebView2");
        Directory.CreateDirectory(webViewDataDir);

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(_webView);

        FormClosing += OnFormClosing;
        Load += OnFormLoad;
    }

    private async void OnFormLoad(object? sender, EventArgs e)
    {
        // Build the user-data folder path
        string webViewDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SIMPE Agent",
            "WebView2");
        Directory.CreateDirectory(webViewDataDir);

        try
        {
            var env = await CoreWebView2Environment.CreateAsync(null, webViewDataDir);
            await _webView.EnsureCoreWebView2Async(env);
            _webView.CoreWebView2.Navigate("http://localhost:5073");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error inicializando WebView2: {ex.GetBaseException().Message}",
                "SIMPE Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _cts.Cancel();
    }
}
