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

        //Motion
        public string WiiShakeX { get; set; } = "F";
        public string WiiShakeY { get; set; } = "F";
        public string WiiShakeZ { get; set; } = "V";
        public bool UseNunchuck { get; set; } = true;

        private static readonly string SavePath = System.IO.Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "ProjNimu", "dolphin_controls.json");

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

            if (!File.Exists(path))
            {
                var fresh = new List<string>
        {
            "[GCPad1]",
            "Device = DInput/0/Keyboard Mouse",
            $"Buttons/A = {GCA}",
            $"Buttons/B = {GCB}",
            $"Buttons/X = {GCX}",
            $"Buttons/Y = {GCY}",
            $"Buttons/Z = {GCZ}",
            $"Buttons/Start = {GCStart}",
            $"Main Stick/Up = {GCStickUp}",
            $"Main Stick/Down = {GCStickDown}",
            $"Main Stick/Left = {GCStickLeft}",
            $"Main Stick/Right = {GCStickRight}",
            $"C-Stick/Up = {GCCStickUp}",
            $"C-Stick/Down = {GCCStickDown}",
            $"C-Stick/Left = {GCCStickLeft}",
            $"C-Stick/Right = {GCCStickRight}",
            $"D-Pad/Up = {GCDPadUp}",
            $"D-Pad/Down = {GCDPadDown}",
            $"D-Pad/Left = {GCDPadLeft}",
            $"D-Pad/Right = {GCDPadRight}",
            $"Triggers/L = {GCL}",
            $"Triggers/R = {GCR}",
        };
                File.WriteAllLines(path, fresh);
                return;
            }

            var lines = new List<string>(File.ReadAllLines(path));

            void SetKey(string key, string value)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].TrimStart().StartsWith(key + " =") ||
                        lines[i].TrimStart().StartsWith(key + "="))
                    {
                        lines[i] = $"{key} = {value}";
                        return;
                    }
                }
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim() == "[GCPad1]")
                    {
                        lines.Insert(i + 1, $"{key} = {value}");
                        return;
                    }
                }
            }

            SetKey("Buttons/A", GCA);
            SetKey("Buttons/B", GCB);
            SetKey("Buttons/X", GCX);
            SetKey("Buttons/Y", GCY);
            SetKey("Buttons/Z", GCZ);
            SetKey("Buttons/Start", GCStart);
            SetKey("Main Stick/Up", GCStickUp);
            SetKey("Main Stick/Down", GCStickDown);
            SetKey("Main Stick/Left", GCStickLeft);
            SetKey("Main Stick/Right", GCStickRight);
            SetKey("C-Stick/Up", GCCStickUp);
            SetKey("C-Stick/Down", GCCStickDown);
            SetKey("C-Stick/Left", GCCStickLeft);
            SetKey("C-Stick/Right", GCCStickRight);
            SetKey("D-Pad/Up", GCDPadUp);
            SetKey("D-Pad/Down", GCDPadDown);
            SetKey("D-Pad/Left", GCDPadLeft);
            SetKey("D-Pad/Right", GCDPadRight);
            SetKey("Triggers/L", GCL);
            SetKey("Triggers/R", GCR);

            File.WriteAllLines(path, lines);
        }

        private void ApplyWiimoteConfig(string configDir)
        {
            var path = Path.Combine(configDir, "WiimoteNew.ini");

            // If file doesn't exist create it fresh
            if (!File.Exists(path))
            {
                var fresh = new List<string>
        {
            "[Wiimote1]",
            "Device = DInput/0/Keyboard Mouse",
            $"Buttons/A = {WiiA}",
            $"Buttons/B = {WiiB}",
            $"Buttons/1 = `{Wii1}`",
            $"Buttons/2 = `{Wii2}`",
            $"Buttons/- = {WiiMinus}",
            $"Buttons/+ = {WiiPlus}",
            $"Buttons/Home = {WiiHome}",
            $"D-Pad/Up = {WiiUp}",
            $"D-Pad/Down = {WiiDown}",
            $"D-Pad/Left = {WiiLeft}",
            $"D-Pad/Right = {WiiRight}",
            $"Shake/X = {WiiShakeX}",
            $"Shake/Y = {WiiShakeY}",
            $"Shake/Z = {WiiShakeZ}",
            $"Extension = Nunchuk",
            $"Nunchuk/Buttons/Z = {NunchuckZ}",
            $"Nunchuk/Stick/Up = {NunchuckUp}",
            $"Nunchuk/Stick/Down = {NunchuckDown}",
            $"Nunchuk/Stick/Left = {NunchuckLeft}",
            $"Nunchuk/Stick/Right = {NunchuckRight}",
            "IR/Up = `Cursor Y-`",
"IR/Down = `Cursor Y+`",
"IR/Left = `Cursor X-`",
"IR/Right = `Cursor X+`",
"IRPassthrough/Object 1 X = `IR Object 1 X`",
"IRPassthrough/Object 1 Y = `IR Object 1 Y`",
"IRPassthrough/Object 1 Size = `IR Object 1 Size`",
"IRPassthrough/Object 2 X = `IR Object 2 X`",
"IRPassthrough/Object 2 Y = `IR Object 2 Y`",
"IRPassthrough/Object 2 Size = `IR Object 2 Size`",
"IRPassthrough/Object 3 X = `IR Object 3 X`",
"IRPassthrough/Object 3 Y = `IR Object 3 Y`",
"IRPassthrough/Object 3 Size = `IR Object 3 Size`",
"IRPassthrough/Object 4 X = `IR Object 4 X`",
"IRPassthrough/Object 4 Y = `IR Object 4 Y`",
"IRPassthrough/Object 4 Size = `IR Object 4 Size`",
"IMUAccelerometer/Up = `Accel Up`",
"IMUAccelerometer/Down = `Accel Down`",
"IMUAccelerometer/Left = `Accel Left`",
"IMUAccelerometer/Right = `Accel Right`",
"IMUAccelerometer/Forward = `Accel Forward`",
"IMUAccelerometer/Backward = `Accel Backward`",
"IMUGyroscope/Pitch Up = `Gyro Pitch Up`",
"IMUGyroscope/Pitch Down = `Gyro Pitch Down`",
"IMUGyroscope/Roll Left = `Gyro Roll Left`",
"IMUGyroscope/Roll Right = `Gyro Roll Right`",
"IMUGyroscope/Yaw Left = `Gyro Yaw Left`",
"IMUGyroscope/Yaw Right = `Gyro Yaw Right`",
"Extension = Nunchuk",
$"Nunchuk/Buttons/C = LCONTROL",
$"Nunchuk/Buttons/Z = {NunchuckZ}",
$"Nunchuk/Stick/Up = {NunchuckUp}",
$"Nunchuk/Stick/Down = {NunchuckDown}",
$"Nunchuk/Stick/Left = {NunchuckLeft}",
$"Nunchuk/Stick/Right = {NunchuckRight}",
"Nunchuk/Stick/Calibration = 100.00 141.42 100.00 141.42 100.00 141.42 100.00 141.42",
"Nunchuk/Shake/X = `Click 2`",
"Nunchuk/Shake/Y = `Click 2`",
"Nunchuk/Shake/Z = `Click 2`",
"[Wiimote2]",
"Device = DInput/0/Keyboard Mouse",
"[Wiimote3]",
"Device = DInput/0/Keyboard Mouse",
"[Wiimote4]",
"Device = DInput/0/Keyboard Mouse",
"[BalanceBoard]",
"Device = DInput/0/Keyboard Mouse",
        };
                File.WriteAllLines(path, fresh);
                return;
            }

            // File exists — only update our specific keys, preserve everything else
            var lines = new List<string>(File.ReadAllLines(path));

            void SetKey(string key, string value)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].TrimStart().StartsWith(key + " =") ||
                        lines[i].TrimStart().StartsWith(key + "="))
                    {
                        lines[i] = $"{key} = {value}";
                        return;
                    }
                }
                // Key not found — add it after [Wiimote1]
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim() == "[Wiimote1]")
                    {
                        lines.Insert(i + 1, $"{key} = {value}");
                        return;
                    }
                }
            }
            // Always preserve these Dolphin-generated lines
            SetKey("IR/Up", "`Cursor Y-`");
            SetKey("IR/Down", "`Cursor Y+`");
            SetKey("IR/Left", "`Cursor X-`");
            SetKey("IR/Right", "`Cursor X+`");
            SetKey("Nunchuk/Buttons/C", "LCONTROL");
            SetKey("Nunchuk/Stick/Calibration", "100.00 141.42 100.00 141.42 100.00 141.42 100.00 141.42");
            SetKey("Nunchuk/Shake/X", "`Click 2`");
            SetKey("Nunchuk/Shake/Y", "`Click 2`");
            SetKey("Nunchuk/Shake/Z", "`Click 2`");
            SetKey("Device", "DInput/0/Keyboard Mouse");
            SetKey("Buttons/A", WiiA);
            SetKey("Buttons/B", WiiB);
            SetKey("Buttons/1", $"`{Wii1}`");
            SetKey("Buttons/2", $"`{Wii2}`");
            SetKey("Buttons/-", WiiMinus);
            SetKey("Buttons/+", WiiPlus);
            SetKey("Buttons/Home", WiiHome);
            SetKey("D-Pad/Up", WiiUp);
            SetKey("D-Pad/Down", WiiDown);
            SetKey("D-Pad/Left", WiiLeft);
            SetKey("D-Pad/Right", WiiRight);
            SetKey("Shake/X", WiiShakeX);
            SetKey("Shake/Y", WiiShakeY);
            SetKey("Shake/Z", WiiShakeZ);
            SetKey("Nunchuk/Buttons/Z", NunchuckZ);
            SetKey("Nunchuk/Stick/Up", NunchuckUp);
            SetKey("Nunchuk/Stick/Down", NunchuckDown);
            SetKey("Nunchuk/Stick/Left", NunchuckLeft);
            SetKey("Nunchuk/Stick/Right", NunchuckRight);

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
                "Device = DInput/0/Keyboard Mouse",
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
                $"Shake/X = `{WiiShakeX}`",
                $"Shake/Y = `{WiiShakeY}`",
                $"Shake/Z = `{WiiShakeZ}`",
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