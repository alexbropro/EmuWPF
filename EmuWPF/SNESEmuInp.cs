using System.IO;
using System.Text.Json;

namespace EmuWPF
{
    public class SNESEmuInp
    {
        public string Up { get; set; } = "Up";
        public string Down { get; set; } = "Down";
        public string Left { get; set; } = "Left";
        public string Right { get; set; } = "Right";
        public string A { get; set; } = "L";
        public string B { get; set; } = "K";
        public string X { get; set; } = "I";
        public string Y { get; set; } = "J";
        public string L { get; set; } = "Q";
        public string R { get; set; } = "E";
        public string Start { get; set; } = "Enter";
        public string Select { get; set; } = "RightShift";

        private static readonly string SavePath = Path.Combine(
            AppContext.BaseDirectory, "snes_controls.json");

        public void Save()
        {
            var json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavePath, json);
        }

        public static SNESEmuInp Load()
        {
            try
            {
                if (File.Exists(SavePath))
                    return JsonSerializer.Deserialize<SNESEmuInp>(
                        File.ReadAllText(SavePath)) ?? new SNESEmuInp();
            }
            catch { }
            return new SNESEmuInp();
        }

        public void ApplyToSnes9x(string snes9xDir)
        {
            var cfgPath = System.IO.Path.Combine(snes9xDir, "snes9x.conf");
            if (!File.Exists(cfgPath)) return;

            var lines = new System.Collections.Generic.List<string>(
                File.ReadAllLines(cfgPath));

            void SetValue(string key, string value)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var trimmed = lines[i].TrimStart();
                    if (trimmed.StartsWith(key))
                    {
                        lines[i] = $"     {key,-32}= {value}";
                        return;
                    }
                }
                lines.Add($"     {key,-32}= {value}");
            }

            // Find or create [Controls\Win] section
            bool sectionFound = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim() == "[Controls\\Win]")
                {
                    sectionFound = true;
                    break;
                }
            }

            if (!sectionFound)
            {
                lines.Add("[Controls\\Win]");
            }

            SetValue("Joypad1:Enabled", "TRUE");
            SetValue("Joypad1:Up", Up);
            SetValue("Joypad1:Down", Down);
            SetValue("Joypad1:Left", Left);
            SetValue("Joypad1:Right", Right);
            SetValue("Joypad1:A", A);
            SetValue("Joypad1:B", B);
            SetValue("Joypad1:X", X);
            SetValue("Joypad1:Y", Y);
            SetValue("Joypad1:L", L);
            SetValue("Joypad1:R", R);
            SetValue("Joypad1:Start", Start);
            SetValue("Joypad1:Select", Select);

            File.WriteAllLines(cfgPath, lines);
        }

        private static string KeyToSnes9x(string key) => key switch
        {
            "Up" => "Up",
            "Down" => "Down",
            "Left" => "Left",
            "Right" => "Right",
            "Enter" => "Return",
            "RightShift" => "RShift",
            "LeftShift" => "LShift",
            "LeftCtrl" => "LCtrl",
            "RightCtrl" => "RCtrl",
            "Space" => "Space",
            "Back" => "BackSpace",
            _ => key
        };
    }
}