using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace EmuWPF
{
    public class DolphinEmuInp
    {
        // GameCube
        public string GCStickUp { get; set; } = "W";
        public string GCStickDown { get; set; } = "S";
        public string GCStickLeft { get; set; } = "A";
        public string GCStickRight { get; set; } = "D";
        public string GCCStickUp { get; set; } = "I";
        public string GCCStickDown { get; set; } = "K";
        public string GCCStickLeft { get; set; } = "J";
        public string GCCStickRight { get; set; } = "L";
        public string GCDPadUp { get; set; } = "Up";
        public string GCDPadDown { get; set; } = "Down";
        public string GCDPadLeft { get; set; } = "Left";
        public string GCDPadRight { get; set; } = "Right";
        public string GCA { get; set; } = "X";
        public string GCB { get; set; } = "Z";
        public string GCX { get; set; } = "C";
        public string GCY { get; set; } = "V";
        public string GCZ { get; set; } = "Q";
        public string GCL { get; set; } = "E";
        public string GCR { get; set; } = "R";
        public string GCStart { get; set; } = "Enter";

        // Wii Remote
        public string WiiUp { get; set; } = "Up";
        public string WiiDown { get; set; } = "Down";
        public string WiiLeft { get; set; } = "Left";
        public string WiiRight { get; set; } = "Right";
        public string WiiA { get; set; } = "X";
        public string WiiB { get; set; } = "Z";
        public string Wii1 { get; set; } = "Q";
        public string Wii2 { get; set; } = "E";
        public string WiiPlus { get; set; } = "Enter";
        public string WiiMinus { get; set; } = "RightShift";
        public string WiiHome { get; set; } = "Escape";
        public string NunchuckZ { get; set; } = "C";
        public string NunchuckUp { get; set; } = "W";
        public string NunchuckDown { get; set; } = "S";
        public string NunchuckLeft { get; set; } = "A";
        public string NunchuckRight { get; set; } = "D";
        public bool UseNunchuck { get; set; } = true;

        private static readonly string SavePath = Path.Combine(
            AppContext.BaseDirectory, "dolphin_controls.json");

        public void Save()
        {
            var json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavePath, json);
        }

        public static DolphinEmuInp Load()
        {
            try
            {
                if (File.Exists(SavePath))
                    return JsonSerializer.Deserialize<DolphinEmuInp>(
                        File.ReadAllText(SavePath)) ?? new DolphinEmuInp();
            }
            catch { }
            return new DolphinEmuInp();
        }

        public void ApplyToDolphin(string dolphinConfigDir)
        {
            ApplyGCProfile(dolphinConfigDir);
            ApplyWiiProfile(dolphinConfigDir);
            ApplyGCPadConfig(dolphinConfigDir);
            ApplyWiimoteConfig(dolphinConfigDir);
        }

        private void ApplyGCPadConfig(string configDir)
        {
            var path = Path.Combine(configDir, "GCPadNew.ini");
            var lines = new List<string>
    {
        "[GCPad1]",
        "Device = Keyboard/0/Keyboard",
        $"Buttons/A = `{GCA}`",
        $"Buttons/B = `{GCB}`",
        $"Buttons/X = `{GCX}`",
        $"Buttons/Y = `{GCY}`",
        $"Buttons/Z = `{GCZ}`",
        $"Buttons/Start = `{GCStart}`",
        $"Main Stick/Up = `{GCStickUp}`",
        $"Main Stick/Down = `{GCStickDown}`",
        $"Main Stick/Left = `{GCStickLeft}`",
        $"Main Stick/Right = `{GCStickRight}`",
        $"C-Stick/Up = `{GCCStickUp}`",
        $"C-Stick/Down = `{GCCStickDown}`",
        $"C-Stick/Left = `{GCCStickLeft}`",
        $"C-Stick/Right = `{GCCStickRight}`",
        $"D-Pad/Up = `{GCDPadUp}`",
        $"D-Pad/Down = `{GCDPadDown}`",
        $"D-Pad/Left = `{GCDPadLeft}`",
        $"D-Pad/Right = `{GCDPadRight}`",
        $"Triggers/L = `{GCL}`",
        $"Triggers/R = `{GCR}`",
    };
            File.WriteAllLines(path, lines);
        }

        private void ApplyWiimoteConfig(string configDir)
        {
            var path = Path.Combine(configDir, "WiimoteNew.ini");
            var lines = new List<string>
    {
        "[Wiimote1]",
        "Device = Keyboard/0/Keyboard",
        $"Buttons/A = `{WiiA}`",
        $"Buttons/B = `{WiiB}`",
        $"Buttons/1 = `{Wii1}`",
        $"Buttons/2 = `{Wii2}`",
        $"Buttons/+ = `{WiiPlus}`",
        $"Buttons/- = `{WiiMinus}`",
        $"Buttons/Home = `{WiiHome}`",
        $"D-Pad/Up = `{WiiUp}`",
        $"D-Pad/Down = `{WiiDown}`",
        $"D-Pad/Left = `{WiiLeft}`",
        $"D-Pad/Right = `{WiiRight}`",
        $"Nunchuk/Buttons/Z = `{NunchuckZ}`",
        $"Nunchuk/Stick/Up = `{NunchuckUp}`",
        $"Nunchuk/Stick/Down = `{NunchuckDown}`",
        $"Nunchuk/Stick/Left = `{NunchuckLeft}`",
        $"Nunchuk/Stick/Right = `{NunchuckRight}`",
    };

            if (UseNunchuck)
            {
                lines.Add($"Nunchuk/Buttons/Z = `{NunchuckZ}`");
                lines.Add($"Nunchuk/Stick/Up = `{NunchuckUp}`");
                lines.Add($"Nunchuk/Stick/Down = `{NunchuckDown}`");
                lines.Add($"Nunchuk/Stick/Left = `{NunchuckLeft}`");
                lines.Add($"Nunchuk/Stick/Right = `{NunchuckRight}`");
                lines.Add("Extension = Nunchuk");
            }
            File.WriteAllLines(path, lines);
        }

        private void ApplyGCProfile(string configDir)
        {
            var profileDir = Path.Combine(configDir, "Profiles", "GCPad");
            Directory.CreateDirectory(profileDir);
            var profilePath = Path.Combine(profileDir, "ProjNimu.ini");

            var lines = new List<string>
            {
                "[Profile]",
                $"Main Stick/Up = `Key {GCStickUp}`",
                $"Main Stick/Down = `Key {GCStickDown}`",
                $"Main Stick/Left = `Key {GCStickLeft}`",
                $"Main Stick/Right = `Key {GCStickRight}`",
                $"C-Stick/Up = `Key {GCCStickUp}`",
                $"C-Stick/Down = `Key {GCCStickDown}`",
                $"C-Stick/Left = `Key {GCCStickLeft}`",
                $"C-Stick/Right = `Key {GCCStickRight}`",
                $"D-Pad/Up = `Key {GCDPadUp}`",
                $"D-Pad/Down = `Key {GCDPadDown}`",
                $"D-Pad/Left = `Key {GCDPadLeft}`",
                $"D-Pad/Right = `Key {GCDPadRight}`",
                $"Buttons/A = `Key {GCA}`",
                $"Buttons/B = `Key {GCB}`",
                $"Buttons/X = `Key {GCX}`",
                $"Buttons/Y = `Key {GCY}`",
                $"Buttons/Z = `Key {GCZ}`",
                $"Triggers/L = `Key {GCL}`",
                $"Triggers/R = `Key {GCR}`",
                $"Buttons/Start = `Key {GCStart}`",
            };

            File.WriteAllLines(profilePath, lines);
        }

        private void ApplyWiiProfile(string configDir)
        {
            var profileDir = Path.Combine(configDir, "Profiles", "Wiimote");
            Directory.CreateDirectory(profileDir);
            var profilePath = Path.Combine(profileDir, "ProjNimu.ini");

            var lines = new List<string>
            {
                "[Profile]",
                $"D-Pad/Up = `Key {WiiUp}`",
                $"D-Pad/Down = `Key {WiiDown}`",
                $"D-Pad/Left = `Key {WiiLeft}`",
                $"D-Pad/Right = `Key {WiiRight}`",
                $"Buttons/A = `Key {WiiA}`",
                $"Buttons/B = `Key {WiiB}`",
                $"Buttons/1 = `Key {Wii1}`",
                $"Buttons/2 = `Key {Wii2}`",
                $"Buttons/+ = `Key {WiiPlus}`",
                $"Buttons/- = `Key {WiiMinus}`",
                $"Buttons/Home = `Key {WiiHome}`",
                $"Nunchuk/Buttons/Z = `Key {NunchuckZ}`",
        $"Nunchuk/Stick/Up = `Key {NunchuckUp}`",
        $"Nunchuk/Stick/Down = `Key {NunchuckDown}`",
        $"Nunchuk/Stick/Left = `Key {NunchuckLeft}`",
        $"Nunchuk/Stick/Right = `Key {NunchuckRight}`",
            };

            File.WriteAllLines(profilePath, lines);
        }
    }
}