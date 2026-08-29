using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EmuWPF
{
    public class N64EmuInp
    {
        // N64 buttons mapped to key names
        public string A { get; set; } = "X";
        public string B { get; set; } = "C";
        public string Z { get; set; } = "Z";
        public string Start { get; set; } = "Enter";
        public string CUp { get; set; } = "8";
        public string CDown { get; set; } = "2";
        public string CLeft { get; set; } = "4";
        public string CRight { get; set; } = "6";
        public string DUp { get; set; } = "Up";
        public string DDown { get; set; } = "Down";
        public string DLeft { get; set; } = "Left";
        public string DRight { get; set; } = "Right";
        public string StickUp { get; set; } = "W";
        public string StickDown { get; set; } = "S";
        public string StickLeft { get; set; } = "A";
        public string StickRight { get; set; } = "D";
        public string L { get; set; } = "Q";
        public string R { get; set; } = "E";

        public bool UseController { get; set; } = false;
        public int ControllerPort { get; set; } = 0; // SDL joystick index

        // PS5 DualSense button mappings (SDL button indices)
        public int BtnA { get; set; } = 1;  // Cross
        public int BtnB { get; set; } = 0;  // Square  
        public int BtnZ { get; set; } = 6;  // L2 (trigger, treated as button)
        public int BtnStart { get; set; } = 9;  // Options
        public int BtnL { get; set; } = 4;  // L1
        public int BtnR { get; set; } = 5;  // R1
        public int BtnCUp { get; set; } = 3;  // Triangle
        public int BtnCDown { get; set; } = 0;  // Cross (alt)
        public int BtnCLeft { get; set; } = 2;  // Circle
        public int BtnCRight { get; set; } = 3;  // Triangle (alt)
        public int AxisX { get; set; } = 0;  // Left stick X
        public int AxisY { get; set; } = 1;  // Left stick Y
        public int DPadUp { get; set; } = 11; // DPad up
        public int DPadDown { get; set; } = 12; // DPad down
        public int DPadLeft { get; set; } = 13; // DPad left
        public int DPadRight { get; set; } = 14; // DPad right

        private static readonly string SavePath = Path.Combine(
            AppContext.BaseDirectory, "n64_controls.json");

        public void Save()
        {
            var json = JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SavePath, json);
        }

        public static N64EmuInp Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    return JsonSerializer.Deserialize<N64EmuInp>(json)
                           ?? new N64EmuInp();
                }
            }
            catch { }
            return new N64EmuInp();
        }

        // Converts the profile into Mupen64Plus input plugin config format
        // Written to mupen64plus.cfg so Mupen picks it up automatically
        public void ApplyToMupen(string mupenConfigDir)
        {
            var cfgPath = Path.Combine(mupenConfigDir, "mupen64plus.cfg");

            if (!File.Exists(cfgPath))
            {
                System.Windows.MessageBox.Show(
                    $"mupen64plus.cfg not found at:\n{cfgPath}\n\nLaunch a game once first so Mupen creates its config.",
                    "Config Not Found");
                return;
            }

            var lines = new List<string>(File.ReadAllLines(cfgPath));
            int sectionStart = -1;
            int sectionEnd = lines.Count;

            // Find [Input-SDL-Control1] section
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim() == "[Input-SDL-Control1]")
                {
                    sectionStart = i;
                    for (int j = i + 1; j < lines.Count; j++)
                    {
                        if (lines[j].Trim().StartsWith("["))
                        {
                            sectionEnd = j;
                            break;
                        }
                    }
                    break;
                }
            }

            // Build new section matching Mupen's exact format
            var section = new List<string>
    {
        "[Input-SDL-Control1]",
        "",
        "# Mupen64Plus SDL Input Plugin config parameter version number.  Please don't change this version number.",
        "version = 2.000000",
        "# Controller configuration mode: 0=Fully Manual, 1=Auto with named SDL Device, 2=Fully automatic",
        "mode = 0",
        "# Specifies which joystick is bound to this controller: -1=No joystick, 0 or more= SDL Joystick number",
        "device = -1",
        "# SDL joystick name (or Keyboard)",
        "name = \"Keyboard\"",
        "# Specifies whether this controller is 'plugged in' to the simulated N64",
        "plugged = True",
        "# Specifies which type of expansion pak is in the controller: 1=None, 2=Mem pak, 4=Transfer pak, 5=Rumble pak",
        "plugin = 2",
        "# If True, then mouse buttons may be used with this controller",
        "mouse = False",
        "# Scaling factor for mouse movements.  For X, Y axes.",
        "MouseSensitivity = \"2.00,2.00\"",
        "# The minimum absolute value of the SDL analog joystick axis to move the N64 controller axis value from 0.  For X, Y axes.",
        "AnalogDeadzone = \"4096,4096\"",
        "# An absolute value of the SDL joystick axis >= AnalogPeak will saturate the N64 controller axis value (at 80).  For X, Y axes. For each axis, this must be greater than the corresponding AnalogDeadzone value",
        "AnalogPeak = \"32768,32768\"",
        "# Digital button configuration mappings",
        $"DPad R = \"key({KeyNameToSdl(DRight)})\"",
        $"DPad L = \"key({KeyNameToSdl(DLeft)})\"",
        $"DPad D = \"key({KeyNameToSdl(DDown)})\"",
        $"DPad U = \"key({KeyNameToSdl(DUp)})\"",
        $"Start = \"key({KeyNameToSdl(Start)})\"",
        $"Z Trig = \"key({KeyNameToSdl(Z)})\"",
        $"B Button = \"key({KeyNameToSdl(B)})\"",
        $"A Button = \"key({KeyNameToSdl(A)})\"",
        $"C Button R = \"key({KeyNameToSdl(CRight)})\"",
        $"C Button L = \"key({KeyNameToSdl(CLeft)})\"",
        $"C Button D = \"key({KeyNameToSdl(CDown)})\"",
        $"C Button U = \"key({KeyNameToSdl(CUp)})\"",
        $"R Trig = \"key({KeyNameToSdl(R)})\"",
        $"L Trig = \"key({KeyNameToSdl(L)})\"",
        "Mempak switch = \"key(44)\"",
        "Rumblepak switch = \"key(46)\"",
        "# Analog axis configuration mappings",
        $"X Axis = \"key({KeyNameToSdl(StickLeft)},{KeyNameToSdl(StickRight)})\"",
        $"Y Axis = \"key({KeyNameToSdl(StickUp)},{KeyNameToSdl(StickDown)})\"",
        ""
    };

            if (UseController)
            {
                section = new List<string>
    {
        "[Input-SDL-Control1]",
        "",
        "# Mupen64Plus SDL Input Plugin config parameter version number.  Please don't change this version number.",
        "version = 2.000000",
        "# Controller configuration mode: 0=Fully Manual, 1=Auto with named SDL Device, 2=Fully automatic",
        "mode = 0",
        "# Specifies which joystick is bound to this controller: -1=No joystick, 0 or more= SDL Joystick number",
        $"device = {ControllerPort}",
        "# SDL joystick name (or Keyboard)",
        "name = \"PS5 Controller\"",
        "# Specifies whether this controller is 'plugged in' to the simulated N64",
        "plugged = True",
        "# Specifies which type of expansion pak is in the controller: 1=None, 2=Mem pak, 4=Transfer pak, 5=Rumble pak",
        "plugin = 5",
        "# If True, then mouse buttons may be used with this controller",
        "mouse = False",
        "# Scaling factor for mouse movements.  For X, Y axes.",
        "MouseSensitivity = \"2.00,2.00\"",
        "# The minimum absolute value of the SDL analog joystick axis to move the N64 controller axis value from 0.  For X, Y axes.",
        "AnalogDeadzone = \"4096,4096\"",
        "# An absolute value of the SDL joystick axis >= AnalogPeak will saturate the N64 controller axis value (at 80).  For X, Y axes. For each axis, this must be greater than the corresponding AnalogDeadzone value",
        "AnalogPeak = \"32768,32768\"",
        "# Digital button configuration mappings",
        $"DPad R = \"button({BtnCRight})\"",
        $"DPad L = \"button({BtnCLeft})\"",
        $"DPad D = \"button({DPadDown})\"",
        $"DPad U = \"button({DPadUp})\"",
        $"Start = \"button({BtnStart})\"",
        $"Z Trig = \"axis(4+)\"",
        $"B Button = \"button({BtnB})\"",
        $"A Button = \"button({BtnA})\"",
        $"C Button R = \"axis(2+)\"",
        $"C Button L = \"axis(2-)\"",
        $"C Button D = \"axis(3+)\"",
        $"C Button U = \"axis(3-)\"",
        $"R Trig = \"button({BtnR})\"",
        $"L Trig = \"button({BtnL})\"",
        "Mempak switch = \"button(15)\"",
        "Rumblepak switch = \"button(16)\"",
        "# Analog axis configuration mappings",
        $"X Axis = \"axis({AxisX}-,{AxisX}+)\"",
        $"Y Axis = \"axis({AxisY}-,{AxisY}+)\"",
        ""
    };
            }
            else
            {
                // existing keyboard section — keep as is
                section = new List<string>
    {
        "[Input-SDL-Control1]",
        "",
        "# Mupen64Plus SDL Input Plugin config parameter version number.  Please don't change this version number.",
        "version = 2.000000",
        "mode = 0",
        "device = -1",
        "name = \"Keyboard\"",
        "plugged = True",
        "plugin = 2",
        "mouse = False",
        "MouseSensitivity = \"2.00,2.00\"",
        "AnalogDeadzone = \"4096,4096\"",
        "AnalogPeak = \"32768,32768\"",
        "# Digital button configuration mappings",
        $"DPad R = \"key({KeyNameToSdl(DRight)})\"",
        $"DPad L = \"key({KeyNameToSdl(DLeft)})\"",
        $"DPad D = \"key({KeyNameToSdl(DDown)})\"",
        $"DPad U = \"key({KeyNameToSdl(DUp)})\"",
        $"Start = \"key({KeyNameToSdl(Start)})\"",
        $"Z Trig = \"key({KeyNameToSdl(Z)})\"",
        $"B Button = \"key({KeyNameToSdl(B)})\"",
        $"A Button = \"key({KeyNameToSdl(A)})\"",
        $"C Button R = \"key({KeyNameToSdl(CRight)})\"",
        $"C Button L = \"key({KeyNameToSdl(CLeft)})\"",
        $"C Button D = \"key({KeyNameToSdl(CDown)})\"",
        $"C Button U = \"key({KeyNameToSdl(CUp)})\"",
        $"R Trig = \"key({KeyNameToSdl(R)})\"",
        $"L Trig = \"key({KeyNameToSdl(L)})\"",
        "Mempak switch = \"key(44)\"",
        "Rumblepak switch = \"key(46)\"",
        "# Analog axis configuration mappings",
        $"X Axis = \"key({KeyNameToSdl(StickLeft)},{KeyNameToSdl(StickRight)})\"",
        $"Y Axis = \"key({KeyNameToSdl(StickUp)},{KeyNameToSdl(StickDown)})\"",
        ""
    };
            }

            if (sectionStart >= 0)
                lines.RemoveRange(sectionStart, sectionEnd - sectionStart);
            else
                sectionStart = lines.Count;

            lines.InsertRange(sectionStart, section);
            File.WriteAllLines(cfgPath, lines);
        }

        // Maps WPF key names to SDL key codes Mupen64Plus understands
        private static string KeyNameToSdl(string key) => key switch
        {
            "A" => "97",
            "B" => "98",
            "C" => "99",
            "D" => "100",
            "E" => "101",
            "F" => "102",
            "G" => "103",
            "H" => "104",
            "I" => "105",
            "J" => "106",
            "K" => "107",
            "L" => "108",
            "M" => "109",
            "N" => "110",
            "O" => "111",
            "P" => "112",
            "Q" => "113",
            "R" => "114",
            "S" => "115",
            "T" => "116",
            "U" => "117",
            "V" => "118",
            "W" => "119",
            "X" => "120",
            "Y" => "121",
            "Z" => "122",
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
            "LeftAlt" => "308",
            "Tab" => "9",
            "Back" => "8",
            "F1" => "282",
            "F2" => "283",
            "F3" => "284",
            "F4" => "285",
            "F5" => "286",
            _ => "0"
        };
    }
}
