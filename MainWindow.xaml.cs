using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;

namespace Mnotas
{
    public partial class MainWindow : Window
    {
        private bool showLineNumbers = true;
        private bool wordWrap = false;
        private double zoom = 1.0;

        public MainWindow()
        {
            InitializeComponent();
            NewTab();
            UpdateStatus();
            // Auto-check for updates on startup (background)
            _ = Task.Run(async () =>
            {
                var info = await UpdateChecker.CheckAsync();
                if (info != null && info.IsUpdateAvailable)
                {
                    Dispatcher.Invoke(() => UpdateChecker.ShowUpdateDialog(info, this));
                }
            });
        }

        private TextEditor CurrentEditor
        {
            get
            {
                if (Tabs.SelectedItem is TabItem ti && ti.Content is TextEditor te)
                    return te;
                return null;
            }
        }

        private TabItem NewTab(string title = "Sin título", string text = "", string filePath = null)
        {
            var editor = new TextEditor
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13 * zoom,
                ShowLineNumbers = showLineNumbers,
                WordWrap = wordWrap,
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text"),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = (Brush)Application.Current.Resources["BgBrush"],
                Foreground = (Brush)Application.Current.Resources["FgBrush"],
            };
            editor.Text = text;
            editor.TextArea.Caret.PositionChanged += (s, e) => UpdateStatus();
            editor.TextChanged += (s, e) => UpdateStatus();
            editor.TextArea.KeyDown += Editor_KeyDown;

            var tab = new TabItem { Header = title, Content = editor, Tag = filePath };
            Tabs.Items.Add(tab);
            Tabs.SelectedItem = tab;
            return tab;
        }

        private void UpdateStatus()
        {
            var ed = CurrentEditor;
            if (ed == null) return;
            var pos = ed.TextArea.Caret.Position;
            StatusText.Text = "Listo";
            PositionText.Text = $"Ln {pos.Line}, Col {pos.Column}";
        }

        // ---------- File ----------
        private void New_Click(object sender, RoutedEventArgs e) => NewTab();

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Todos los archivos (*.*)|*.*|Texto (*.txt)|*.txt|C# (*.cs)|*.cs|JavaScript (*.js)|*.js|Python (*.py)|*.py|HTML (*.html)|*.html|CSS (*.css)|*.css|JSON (*.json)|*.json|XML (*.xml)|*.xml|SQL (*.sql)|*.sql|Markdown (*.md)|*.md",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (var f in dlg.FileNames)
                {
                    try
                    {
                        var text = File.ReadAllText(f, Encoding.UTF8);
                        var name = Path.GetFileName(f);
                        var tab = NewTab(name, text, f);
                        DetectLanguage(tab, f);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo abrir {f}:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var tab = Tabs.SelectedItem as TabItem;
            if (tab == null) return;
            var path = tab.Tag as string;
            if (string.IsNullOrEmpty(path))
            {
                SaveAs_Click(sender, e);
                return;
            }
            try
            {
                var ed = (TextEditor)tab.Content;
                File.WriteAllText(path, ed.Text, Encoding.UTF8);
                tab.Header = Path.GetFileName(path);
                StatusText.Text = $"Guardado: {path}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo guardar:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            var ed = CurrentEditor;
            if (ed == null) return;
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Texto (*.txt)|*.txt|Todos (*.*)|*.*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dlg.FileName, ed.Text, Encoding.UTF8);
                    var tab = Tabs.SelectedItem as TabItem;
                    tab.Tag = dlg.FileName;
                    tab.Header = Path.GetFileName(dlg.FileName);
                    StatusText.Text = $"Guardado: {dlg.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo guardar:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (Tabs.Items.Count <= 1)
            {
                NewTab();
            }
            if (Tabs.SelectedItem is TabItem ti)
            {
                Tabs.Items.Remove(ti);
                if (Tabs.Items.Count == 0) NewTab();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        // ---------- Edit ----------
        private void Undo_Click(object sender, RoutedEventArgs e) => CurrentEditor?.Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => CurrentEditor?.Redo();
        private void Cut_Click(object sender, RoutedEventArgs e) => CurrentEditor?.Cut();
        private void Copy_Click(object sender, RoutedEventArgs e) => CurrentEditor?.Copy();
        private void Paste_Click(object sender, RoutedEventArgs e) => CurrentEditor?.Paste();

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            var ed = CurrentEditor;
            if (ed == null) return;
            var dlg = new FindReplaceDialog(ed, find: true);
            dlg.Owner = this;
            dlg.Show();
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            var ed = CurrentEditor;
            if (ed == null) return;
            var dlg = new FindReplaceDialog(ed, find: false);
            dlg.Owner = this;
            dlg.Show();
        }

        // ---------- View ----------
        private void ToggleLineNumbers_Click(object sender, RoutedEventArgs e)
        {
            showLineNumbers = !showLineNumbers;
            foreach (TabItem ti in Tabs.Items)
                if (ti.Content is TextEditor te) te.ShowLineNumbers = showLineNumbers;
        }

        private void ToggleWordWrap_Click(object sender, RoutedEventArgs e)
        {
            wordWrap = !wordWrap;
            foreach (TabItem ti in Tabs.Items)
                if (ti.Content is TextEditor te) te.WordWrap = wordWrap;
        }

        private void ThemeLight_Click(object sender, RoutedEventArgs e) => ApplyTheme(false);
        private void ThemeDark_Click(object sender, RoutedEventArgs e) => ApplyTheme(true);

        private void ApplyTheme(bool dark)
        {
            var bg = dark ? "#FF1E1E1E" : "#FFFFFFFF";
            var fg = dark ? "#FFE8E8E8" : "#FF1F1F1F";
            var alt = dark ? "#FF252526" : "#FFF3F3F3";
            Resources["BgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            Resources["BgAltBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(alt));
            Resources["FgBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg));
            foreach (TabItem ti in Tabs.Items)
            {
                if (ti.Content is TextEditor te)
                {
                    te.Background = (Brush)Resources["BgBrush"];
                    te.Foreground = (Brush)Resources["FgBrush"];
                }
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) { zoom += 0.1; ApplyZoom(); }
        private void ZoomOut_Click(object sender, RoutedEventArgs e) { zoom = Math.Max(0.5, zoom - 0.1); ApplyZoom(); }
        private void ApplyZoom()
        {
            foreach (TabItem ti in Tabs.Items)
                if (ti.Content is TextEditor te) te.FontSize = 13 * zoom;
        }

        // ---------- Language ----------
        private void Lang_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string lang)
            {
                var def = HighlightingManager.Instance.GetDefinition(lang == "Text" ? "Text" : lang);
                var ed = CurrentEditor;
                if (ed != null && def != null) ed.SyntaxHighlighting = def;
            }
        }

        private void DetectLanguage(TabItem tab, string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            string lang = ext switch
            {
                ".cs" => "C#",
                ".js" => "JavaScript",
                ".py" => "Python",
                ".html" or ".htm" => "HTML",
                ".css" => "CSS",
                ".json" => "JSON",
                ".xml" => "XML",
                ".sql" => "SQL",
                ".md" => "Markdown",
                _ => "Text"
            };
            if (tab.Content is TextEditor te)
            {
                var def = HighlightingManager.Instance.GetDefinition(lang);
                if (def != null) te.SyntaxHighlighting = def;
            }
        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateStatus();

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Buscando actualizaciones...";
            var info = await UpdateChecker.CheckAsync();
            if (info == null)
            {
                MessageBox.Show("No se pudo comprobar. Verifica tu conexión.", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusText.Text = "Listo";
                return;
            }
            if (info.IsUpdateAvailable)
            {
                UpdateChecker.ShowUpdateDialog(info, this);
            }
            else
            {
                MessageBox.Show($"Ya tienes la última versión ({UpdateChecker.CurrentVersion}).", "Actualizaciones", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            StatusText.Text = "Listo";
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Mnotas v{UpdateChecker.CurrentVersion}\nEditor de texto moderno\n\n© 2026 Diseñado por MSoft\nmsoft.com.es", "Acerca de Mnotas", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.N) { New_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.O) { Open_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.S) { Save_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.F) { Find_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.H) { Replace_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.Add || e.Key == Key.OemPlus) { ZoomIn_Click(null, null); e.Handled = true; }
                else if (e.Key == Key.Subtract || e.Key == Key.OemMinus) { ZoomOut_Click(null, null); e.Handled = true; }
            }
        }
    }

    // Simple Find/Replace dialog
    public class FindReplaceDialog : Window
    {
        private readonly TextEditor editor;
        private readonly bool findOnly;
        private TextBox findBox;
        private TextBox replaceBox;

        public FindReplaceDialog(TextEditor editor, bool find)
        {
            this.editor = editor;
            this.findOnly = find;
            Title = find ? "Buscar" : "Reemplazar";
            Width = 420; Height = find ? 140 : 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var grid = new Grid { Margin = new Thickness(12) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            if (!find) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lblFind = new TextBlock { Text = "Buscar:", Margin = new Thickness(0,0,0,4) };
            Grid.SetRow(lblFind, 0); grid.Children.Add(lblFind);
            findBox = new TextBox { Margin = new Thickness(0,0,0,8) };
            Grid.SetRow(findBox, 1); grid.Children.Add(findBox);

            int row = 2;
            if (!find)
            {
                var lblRep = new TextBlock { Text = "Reemplazar:", Margin = new Thickness(0,0,0,4) };
                Grid.SetRow(lblRep, 2); grid.Children.Add(lblRep);
                replaceBox = new TextBox { Margin = new Thickness(0,0,0,8) };
                Grid.SetRow(replaceBox, 3); grid.Children.Add(replaceBox);
                row = 4;
            }

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnFind = new Button { Content = "Buscar siguiente", Padding = new Thickness(10,4,10,4), Margin = new Thickness(0,0,6,0) };
            btnFind.Click += (s, e) => FindNext();
            btnPanel.Children.Add(btnFind);
            if (!find)
            {
                var btnRep = new Button { Content = "Reemplazar", Padding = new Thickness(10,4,10,4), Margin = new Thickness(0,0,6,0) };
                btnRep.Click += (s, e) => ReplaceOne();
                btnPanel.Children.Add(btnRep);
                var btnAll = new Button { Content = "Reemplazar todo", Padding = new Thickness(10,4,10,4) };
                btnAll.Click += (s, e) => ReplaceAll();
                btnPanel.Children.Add(btnAll);
            }
            Grid.SetRow(btnPanel, row); grid.Children.Add(btnPanel);

            Content = grid;
        }

        private void FindNext()
        {
            var term = findBox.Text;
            if (string.IsNullOrEmpty(term)) return;
            var text = editor.Text;
            var start = editor.SelectionStart + editor.SelectionLength;
            var idx = text.IndexOf(term, start, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) idx = text.IndexOf(term, 0, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                editor.Select(idx, term.Length);
                editor.ScrollToLine(editor.Document.GetLineByOffset(idx).LineNumber);
            }
            else
            {
                MessageBox.Show("No se encontró.", "Buscar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ReplaceOne()
        {
            if (editor.SelectionLength > 0 && string.Equals(editor.SelectedText, findBox.Text, StringComparison.OrdinalIgnoreCase))
            {
                editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, replaceBox.Text);
            }
            FindNext();
        }

        private void ReplaceAll()
        {
            var term = findBox.Text;
            if (string.IsNullOrEmpty(term)) return;
            var count = 0;
            var idx = 0;
            while ((idx = editor.Text.IndexOf(term, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                editor.Document.Replace(idx, term.Length, replaceBox.Text);
                idx += replaceBox.Text.Length;
                count++;
            }
            MessageBox.Show($"Se reemplazaron {count} ocurrencias.", "Reemplazar", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
