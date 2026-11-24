using System.Windows.Controls;
using System.Windows;

namespace Mnotas
{
    public partial class AiConfigWindow : Window
    {
        public AiConfigWindow()
        {
            InitializeComponent();
            // Cargar configuraciones existentes
            AiSettings.Load();
            ApiKeyTextBox.Text = AiSettings.ApiKey;
            ModelComboBox.SelectedItem = FindItemByContent(ModelComboBox, AiSettings.Model);
            EndpointTextBox.Text = AiSettings.Endpoint;
        }

        private static ComboBoxItem FindItemByContent(ComboBox cb, string content)
        {
            foreach (var item in cb.Items)
            {
                if (item is ComboBoxItem cbi && cbi.Content.ToString() == content)
                    return cbi;
            }
            return null;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            AiSettings.ApiKey = ApiKeyTextBox.Text;
            AiSettings.Model = (ModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "deepseek-chat";
            AiSettings.Endpoint = EndpointTextBox.Text;
            AiSettings.Save();
            MessageBox.Show("Configuración guardada correctamente.", "IA", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}