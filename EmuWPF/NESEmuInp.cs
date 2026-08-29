using System.IO;
using System.Text.Json;

namespace EmuWPF
{
    public class NESEmuInp
    {
        public string Up { get; set; } = "Up";
        public string Down { get; set; } = "Down";
        public string Left { get; set; } = "Left";
        public string Right { get; set; } = "Right";
        public string A { get; set; } = "X";
        public string B { get; set; } = "Z";
        public string Start { get; set; } = "Enter";
        public string Select { get; set; } = "S";

        private static readonly string SavePath = Path.Combine(
            AppContext.BaseDirectory, "nes_controls.json");

        public void Save()
        {
            var json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavePath, json);
        }

        public static NESEmuInp Load()
        {
            try
            {
                if (File.Exists(SavePath))
                    return JsonSerializer.Deserialize<NESEmuInp>(
                        File.ReadAllText(SavePath)) ?? new NESEmuInp();
            }
            catch { }
            return new NESEmuInp();
        }

        public void ApplyToFCEUX(string fceuExeDir)
        {
            var cfgPath = Path.Combine(fceuExeDir, "fceux.cfg");
            if (!File.Exists(cfgPath)) return;

            // Button order: A, B, Select, Start, Up, Down, Left, Right
            var buttonKeys = new[]
            {
        KeyToFCEUXScan(A),
        KeyToFCEUXScan(B),
        KeyToFCEUXScan(Select),
        KeyToFCEUXScan(Start),
        KeyToFCEUXScan(Up),
        KeyToFCEUXScan(Down),
        KeyToFCEUXScan(Left),
        KeyToFCEUXScan(Right),
    };

            // Build binary: 8 buttons, 92 bytes each, total 668 bytes
            const int stride = 92;
            var data = new byte[668];

            for (int i = 0; i < buttonKeys.Length; i++)
            {
                int offset = i * stride;
                if (offset >= 668) break;

                // Key scan code at offset+8
                var keyBytes = BitConverter.GetBytes(buttonKeys[i]);
                if (offset + 12 <= 668)
                    Array.Copy(keyBytes, 0, data, offset + 8, 4);

                // Active flag at offset+24
                if (offset + 28 <= 668)
                    data[offset + 24] = 1;
            }

            var b64 = Convert.ToBase64String(data);

            // Replace GamePadConfig_V2 line in cfg
            var lines = new System.Collections.Generic.List<string>(
                File.ReadAllLines(cfgPath));

            bool found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("GamePadConfig_V2"))
                {
                    lines[i] = $"GamePadConfig_V2 base64:{b64}";
                    found = true;
                    break;
                }
            }
            if (!found)
                lines.Add($"GamePadConfig_V2 base64:{b64}");

            File.WriteAllLines(cfgPath, lines);
        }

        // PC keyboard scan codes (set 1) — what FCEUX actually uses
        private static int KeyToFCEUXScan(string key) => key switch
        {
            "A" => 0x1E,
            "B" => 0x30,
            "C" => 0x2E,
            "D" => 0x20,
            "E" => 0x12,
            "F" => 0x21,
            "G" => 0x22,
            "H" => 0x23,
            "I" => 0x17,
            "J" => 0x24,
            "K" => 0x25,
            "L" => 0x26,
            "M" => 0x32,
            "N" => 0x31,
            "O" => 0x18,
            "P" => 0x19,
            "Q" => 0x10,
            "R" => 0x13,
            "S" => 0x1F,
            "T" => 0x14,
            "U" => 0x16,
            "V" => 0x2F,
            "W" => 0x11,
            "X" => 0x2D,
            "Y" => 0x15,
            "Z" => 0x2C,
            "Up" => 0xC8,
            "Down" => 0xD0,
            "Left" => 0xCB,
            "Right" => 0xCD,
            "Enter" => 0x1C,
            "Space" => 0x39,
            "LeftShift" => 0x2A,
            "RightShift" => 0x36,
            "LeftCtrl" => 0x1D,
            "RightCtrl" => 0x1D,
            "LeftAlt" => 0x38,
            "Tab" => 0x0F,
            "Back" => 0x0E,
            "Escape" => 0x01,
            "F1" => 0x3B,
            "F2" => 0x3C,
            "F3" => 0x3D,
            "F4" => 0x3E,
            "F5" => 0x3F,
            _ => 0x00
        };

        private static int KeyToSDL(string key) => key switch
        {
            "Up" => 273,
            "Down" => 274,
            "Left" => 276,
            "Right" => 275,
            "Enter" => 13,
            "Space" => 32,
            "LeftShift" => 304,
            "RightShift" => 303,
            "LeftCtrl" => 306,
            "RightCtrl" => 305,
            "Tab" => 9,
            "Back" => 8,
            "F1" => 282,
            "F2" => 283,
            "Escape" => 27,
            _ => key.Length == 1 ? (int)char.ToLower(key[0]) : 0
        };

        private static string KeyToFCEUX(string key) => key switch
        {
            "Up" => "273",
            "Down" => "274",
            "Left" => "276",
            "Right" => "275",
            "Enter" => "13",
            "Space" => "32",
            "LeftShift" => "304",
            "RightShift" => "303",
            "LeftCtrl" => "306",
            "RightCtrl" => "305",
            "Tab" => "9",
            "Back" => "8",
            _ => ((int)key[0]).ToString()
        };
    }
}