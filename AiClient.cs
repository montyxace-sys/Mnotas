using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Mnotas
{
    public static class AiSettings
    {
        private static readonly string ConfigPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Mnotas", "ai_config.json");

        public static string ApiKey { get; set; } = "";
        public static string Model { get; set; } = "deepseek-chat";
        public static string Endpoint { get; set; } = "https://api.deepseek.com/v1/chat/completions";

        public static void Load()
        {
            try
            {
                if (System.IO.File.Exists(ConfigPath))
                {
                    var json = System.IO.File.ReadAllText(ConfigPath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("ApiKey", out var k)) ApiKey = k.GetString() ?? "";
                    if (root.TryGetProperty("Model", out var m)) Model = m.GetString() ?? "deepseek-chat";
                    if (root.TryGetProperty("Endpoint", out var e)) Endpoint = e.GetString() ?? "https://api.deepseek.com/v1/chat/completions";
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(ConfigPath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                var obj = new { ApiKey, Model, Endpoint };
                var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }

    public class AiClient
    {
        private readonly HttpClient _http = new HttpClient();
        private const string DefaultEndpoint = "https://api.deepseek.com/v1/chat/completions";

        public async Task<string> ChatAsync(string systemPrompt, string userMessage, string context = null)
        {
            AiSettings.Load();
            if (string.IsNullOrWhiteSpace(AiSettings.ApiKey))
                return "Error: No se ha configurado la API Key de IA. Ve a IA → Configurar API Key.";

            var endpoint = string.IsNullOrWhiteSpace(AiSettings.Endpoint) ? DefaultEndpoint : AiSettings.Endpoint;
            var messages = new System.Collections.Generic.List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Add(new { role = "system", content = systemPrompt });
            if (!string.IsNullOrWhiteSpace(context))
                messages.Add(new { role = "system", content = "Contexto del documento:\n" + context });
            messages.Add(new { role = "user", content = userMessage });

            var body = new
            {
                model = AiSettings.Model,
                messages = messages,
                temperature = 0.7,
                max_tokens = 2048
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + AiSettings.ApiKey);

            try
            {
                var resp = await _http.PostAsync(endpoint, content);
                var respText = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    return $"Error HTTP {resp.StatusCode}: {respText}";

                using var doc = JsonDocument.Parse(respText);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                }
                return "Sin respuesta.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
