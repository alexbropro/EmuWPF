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

        public void ApplyToSnes9x(string snes9xConfigDir)
        {
            var cfgPath = Path.Combine(snes9xConfigDir, "snes9x.conf");
            if (!File.Exists(cfgPath)) return;

            var lines = new System.Collections.Generic.List<string>(
                File.ReadAllLines(cfgPath));

            void SetKey(string key, string value)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith(key + " = ") || lines[i].StartsWith(key + "="))
                    {
                        lines[i] = $"{key} = {value}";
                        return;
                    }
                }
                lines.Add($"{key} = {value}");
            }

            SetKey("Joypad0 Up", KeyToSnes9x(Up));
            SetKey("Joypad0 Down", KeyToSnes9x(Down));
            SetKey("Joypad0 Left", KeyToSnes9x(Left));
            SetKey("Joypad0 Right", KeyToSnes9x(Right));
            SetKey("Joypad0 A", KeyToSnes9x(A));
            SetKey("Joypad0 B", KeyToSnes9x(B));
            SetKey("Joypad0 X", KeyToSnes9x(X));
            SetKey("Joypad0 Y", KeyToSnes9x(Y));
            SetKey("Joypad0 L", KeyToSnes9x(L));
            SetKey("Joypad0 R", KeyToSnes9x(R));
            SetKey("Joypad0 Start", KeyToSnes9x(Start));
            SetKey("Joypad0 Select", KeyToSnes9x(Select));

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