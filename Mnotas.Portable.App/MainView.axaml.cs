using System.Diagnostics;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Mnotas.Core;

namespace Mnotas.Portable.App;

/// <summary>Editor portable compartido por escritorio, Android e iOS.</summary>
public partial class MainView : UserControl
{
    private static readonly FilePickerFileType TextType = new("Documentos de texto")
    {
        Patterns = new[] { "*.txt", "*.md", "*.cs", "*.js", "*.py", "*.json", "*.xml", "*.html", "*.css", "*.sql", "*.*" }
    };

    private string? _path;

    public MainView()
    {
        InitializeComponent();
        Editor.TextChanged += (_, _) => UpdateStats();
        Editor.KeyUp += (_, _) => UpdateStats();
        UpdateStats();
    }

    private void UpdateStats()
    {
        var stats = TextOperations.GetStatistics(Editor.Text ?? string.Empty);
        StatsText.Text = $"{stats.Lines} línea(s), {stats.Words} palabras, {stats.Characters} caracteres";
    }

    private void New_Click(object? sender, RoutedEventArgs e)
    {
        Editor.Text = string.Empty;
        _path = null;
        StatusText.Text = "Nuevo documento";
        Editor.Focus();
        e.Handled = true;
    }

    private async void Open_Click(object? sender, RoutedEventArgs e)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is not null && storage.CanOpen)
        {
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Abrir nota",
                AllowMultiple = false,
                FileTypeFilter = new[] { TextType }
            });
            if (files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (path is null)
            {
                StatusText.Text = "Este archivo no se puede abrir como ruta local.";
                return;
            }
            ReadPath(path);
            e.Handled = true;
            return;
        }

        // En móvil no siempre hay selector: se usa el documento local de la app.
        var local = LocalDocumentPath();
        if (File.Exists(local)) ReadPath(local);
        else StatusText.Text = "En este dispositivo usa «Guardar» y después «Abrir» para volver al documento local.";
        e.Handled = true;
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_path))
        {
            WritePath(_path!);
            e.Handled = true;
            return;
        }

        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is not null && storage.CanSave)
        {
            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar nota",
                SuggestedFileName = "notas.txt",
                DefaultExtension = ".txt",
                FileTypeChoices = new[] { TextType }
            });
            if (file is null) return;
            var path = file.TryGetLocalPath();
            if (path is null)
            {
                StatusText.Text = "Este destino no permite guardar como archivo local.";
                return;
            }
            WritePath(path);
            e.Handled = true;
            return;
        }

        WritePath(LocalDocumentPath());
        e.Handled = true;
    }

    private void ReadPath(string path)
    {
        try
        {
            Editor.Text = File.ReadAllText(path, Encoding.UTF8);
            _path = path;
            StatusText.Text = $"Abierto: {Path.GetFileName(path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"No se pudo abrir: {ex.Message}";
        }
        finally
        {
            UpdateStats();
        }
    }

    private void WritePath(string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, Editor.Text ?? string.Empty, Encoding.UTF8);
            _path = path;
            StatusText.Text = $"Guardado: {Path.GetFileName(path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"No se pudo guardar: {ex.Message}";
        }
    }

    private static string LocalDocumentPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Mnotas", "notas.txt");

    private void Duplicate_Click(object? sender, RoutedEventArgs e)
    {
        var result = TextOperations.DuplicateLine(Editor.Text ?? string.Empty, Editor.CaretIndex);
        Editor.Text = result.Text;
        Editor.CaretIndex = Math.Clamp(result.CaretOffset, 0, Editor.Text?.Length ?? 0);
        Editor.Focus();
        StatusText.Text = "Línea duplicada";
        UpdateStats();
        e.Handled = true;
    }

    private void Editor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.D && e.KeyModifiers == KeyModifiers.Control)
        {
            Duplicate_Click(sender, e);
            e.Handled = true;
        }
    }

    private void Donate_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var url = button.Tag?.ToString();
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            StatusText.Text = $"Abre este enlace para apoyar el proyecto: {url}";
        }
        e.Handled = true;
    }
}
