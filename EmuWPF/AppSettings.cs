using System;
using System.Text.Json;
using System.IO;

namespace EmuWPF
{
    public class AppSettings
    {
        // Theme
        public string AccentColour { get; set; } = "#00F0FF";
        public string BackgroundColour { get; set; } = "#0A0A12";

        // Audio
        public int Volume { get; set; } = 100;
        public bool Muted { get; set; } = false;

        // Display
        public string Resolution { get; set; } = "800x600";
        public bool Fullscreen { get; set; } = false;

        // ── Save / Load ──────────────────────────────────────────────────

        private static readonly string SavePath = Path.Combine(
            AppContext.BaseDirectory, "settings.json");

        public void Save()
        {
            var json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavePath, json);
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }
    }
}