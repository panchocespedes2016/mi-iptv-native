using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MiIptv.App.Services;
using MiIptv.Core;

namespace MiIptv.App.Views;

public partial class SettingsView : UserControl
{
    private Profile? _editing;
    private string? _m3uContent;
    private bool _loadingTimeshift;

    private static readonly (string Label, int Minutes)[] TimeshiftOptions =
    {
        ("Desactivado", 0), ("15 minutos", 15), ("30 minutos", 30),
        ("60 minutos", 60), ("120 minutos", 120),
    };

    public SettingsView()
    {
        InitializeComponent();
        RefreshList();
        _loadingTimeshift = true;
        TimeshiftBox.ItemsSource = TimeshiftOptions.Select(o => o.Label).ToList();
        var cur = AppState.Current.Config.Settings.TimeshiftMinutes;
        TimeshiftBox.SelectedIndex = Math.Max(0, Array.FindIndex(TimeshiftOptions, o => o.Minutes == cur));
        _loadingTimeshift = false;
    }

    private void RefreshList()
    {
        ProfileList.ItemsSource = null;
        ProfileList.ItemsSource = AppState.Current.Config.Profiles;
        ProfileList.DisplayMemberPath = "Name";
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        _editing = new Profile();
        _m3uContent = null;
        FileLabel.Text = "";
        ShowForm(_editing);
    }

    private void ProfileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileList.SelectedItem is Profile p) { _editing = p; ShowForm(p); }
    }

    private void ShowForm(Profile p)
    {
        FName.Text = p.Name;
        FType.SelectedIndex = p.Type == "m3u" ? 1 : 0;
        FHost.Text = p.Host; FUser.Text = p.User; FPass.Password = p.Pass;
        FUrl.Text = p.Url ?? "";
        _m3uContent = p.Content;
        FileLabel.Text = p.Content is not null ? "Archivo cargado ✓" : "";
        UpdateTypeFields();
        FormPanel.Visibility = Visibility.Visible;
        TestResult.Text = "";
    }

    private void FType_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateTypeFields();

    private void UpdateTypeFields()
    {
        var isM3U = FType.SelectedIndex == 1;
        XtreamFields.Visibility = isM3U ? Visibility.Collapsed : Visibility.Visible;
        M3uFields.Visibility = isM3U ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Listas m3u (*.m3u;*.m3u8)|*.m3u;*.m3u8|Todos (*.*)|*.*" };
        if (dlg.ShowDialog() == true)
        {
            _m3uContent = File.ReadAllText(dlg.FileName);
            FileLabel.Text = System.IO.Path.GetFileName(dlg.FileName) + " ✓";
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        FormPanel.Visibility = Visibility.Collapsed;
        ProfileList.SelectedItem = null;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_editing is null) return;
        var st = AppState.Current;
        _editing.Name = FName.Text.Trim();
        _editing.Type = FType.SelectedIndex == 1 ? "m3u" : "xtream";
        _editing.Host = FHost.Text.Trim().TrimEnd('/');
        _editing.User = FUser.Text.Trim();
        _editing.Pass = FPass.Password;
        _editing.Url = string.IsNullOrWhiteSpace(FUrl.Text) ? null : FUrl.Text.Trim();
        _editing.Content = _m3uContent;
        if (string.IsNullOrEmpty(_editing.Name)) { TestResult.Text = "Ponle un nombre al perfil."; return; }
        if (!st.Config.Profiles.Any(p => p.Id == _editing.Id)) st.Config.Profiles.Add(_editing);
        if (st.ActiveProfile is null) st.SetActiveProfile(_editing);
        st.Save();
        RefreshList();
        FormPanel.Visibility = Visibility.Collapsed;
        TestResult.Text = "Perfil guardado ✓";
    }

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileList.SelectedItem is Profile p)
        {
            AppState.Current.SetActiveProfile(p);
            TestResult.Text = $"Perfil activo: {p.Name}";
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileList.SelectedItem is not Profile p) return;
        var st = AppState.Current;
        st.Config.Profiles.Remove(p);
        if (st.ActiveProfile?.Id == p.Id && st.Config.Profiles.Count > 0)
            st.SetActiveProfile(st.Config.Profiles[0]);
        st.Save();
        RefreshList();
        FormPanel.Visibility = Visibility.Collapsed;
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        TestResult.Text = "Probando…";
        try
        {
            var client = new XtreamClient(FHost.Text.Trim().TrimEnd('/'), FUser.Text.Trim(), FPass.Password);
            var ok = await client.LoginAsync();
            TestResult.Text = ok ? "✓ Conexión exitosa" : "✗ Usuario o contraseña inválidos";
        }
        catch (Exception ex) { TestResult.Text = "✗ " + ex.Message; }
    }

    private void Timeshift_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingTimeshift || TimeshiftBox.SelectedIndex < 0) return;
        AppState.Current.Config.Settings.TimeshiftMinutes = TimeshiftOptions[TimeshiftBox.SelectedIndex].Minutes;
        AppState.Current.Save();
    }
}
