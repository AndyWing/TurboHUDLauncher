using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace TurboHUDLauncher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly static string TARGET_PROCESS_NAME = "TurboHUD";
        private readonly static string KEY_HUD_PATH = "HUD_PATH";

        private string HUDPath;

        private readonly Timer CheckProcessStateTimer;
        private readonly Properties LocalConfigProp;

        public MainWindow()
        {
            InitializeComponent();

            CheckProcessStateTimer = new Timer(1000)
            {
                AutoReset = true
            };
            CheckProcessStateTimer.Elapsed += CheckProcessTimer_Elapsed;
            CheckProcessStateTimer.Start();

            button_choose_hud.Click += Button_choose_hud_Click;
            button_start_stop_hud.Click += Button_start_stop_hud_Click;

            string curAppDir = Directory.GetCurrentDirectory();

            LocalConfigProp = new Properties(curAppDir + "/config.ini");

            HUDPath = LocalConfigProp.get(KEY_HUD_PATH);
            // read relative path
            if (HUDPath == null)
            {
                HUDPath = Directory.EnumerateFiles(curAppDir).TakeWhile(name => name.EndsWith(TARGET_PROCESS_NAME+".exe")).FirstOrDefault();
            }

            if (HUDPath != null &&  HUDPath.Length != 0)
            {
                text_hud_path.Text = HUDPath;
            }
           
        }

        private void Button_start_stop_hud_Click(object sender, RoutedEventArgs e)
        {
            if (IsProcessRunning(TARGET_PROCESS_NAME, out Process p))
            {
                p.Kill();
                return;
            }

            if (HUDPath == null || HUDPath.Length == 0)
            {
                MessageBox.Show(TurboHUDLauncher.Properties.Resources.path_not_set_hint);
                return;
            }

            try
            {
                var process = Process.Start(HUDPath);
                if (process != null)
                {
                    //var processId = process.Id;
                    process.Exited += Hud_Process_Exited;
                }
            }
            catch (Exception)
            {
                MessageBox.Show(TurboHUDLauncher.Properties.Resources.cannot_start_process);
                Console.WriteLine("Unable to start the hud process");
            }
        }

        private void CheckProcessTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            bool hudRunning = IsProcessRunning(TARGET_PROCESS_NAME);
            button_start_stop_hud.Dispatcher.Invoke(() => {
                try
                {
                    button_start_stop_hud.Content = hudRunning ?
                            TurboHUDLauncher.Properties.Resources.process_stop :
                            TurboHUDLauncher.Properties.Resources.process_start;
                }
                catch (Exception)
                {
                }
            });
        }

        private void Hud_Process_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Hud_Process_Exited");
        }

        private void Button_choose_hud_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) == true)
            {
                HUDPath = dialog.FileName;
                text_hud_path.Text = dialog.FileName;
                LocalConfigProp.set(KEY_HUD_PATH, dialog.FileName);
            }
        }

        public static bool IsProcessRunning(string name)
        {
            return IsProcessRunning(name, out Process p);
        }

        public static bool IsProcessRunning(string name, out Process process)
        {
            try
            {
                Process[] localProcesses = Process.GetProcessesByName(name);
                if (localProcesses.Length < 1)
                {
                    process = null;
                    return false;
                }
                process = localProcesses[0];
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
        }

        private class Properties
        {
            private readonly string FilePath;
            private readonly Dictionary<string, string> Dictionary;

            public Properties(string FilePath)
            {
                this.FilePath = FilePath;
                Dictionary = new Dictionary<string, string>();
                try
                {
                    foreach (var row in File.ReadAllLines(FilePath))
                    {
                        if (!row.StartsWith("#"))
                        {
                            string[] lineSplit = row.Split('=');
                            var key = lineSplit[0];
                            var value = string.Join("=", lineSplit.Skip(1).ToArray());
                            Dictionary.Add(key, value);
                            Console.WriteLine("k: " + key + ", v: " + value);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Cannot load the config file");
                }
            }
            public string get(string key)
            {
                return get(key, null);
            }

            public string get(string key, string defValue)
            {
                if (Dictionary.TryGetValue(key, out string outV))
                {
                    return outV;
                }
                else
                {
                    return defValue;
                }

            }

            public void set(string key, string value)
            {
                if (Dictionary.ContainsKey(key))
                {
                    Dictionary.Remove(key);
                }
                Dictionary.Add(key, value);

                List<string> vs = new List<string>();
                foreach (var kv in Dictionary)
                {
                    vs.Add(kv.Key + "=" + kv.Value);
                }
                
                File.WriteAllLines(FilePath, vs.ToArray());
            }
        }

        static Brush FocusBrush = Brushes.AliceBlue;
        static Brush LostFocusBrush = Brushes.AliceBlue;
        private void Button_start_stop_hud_LostFocus(object sender, RoutedEventArgs e)
        {
            //(()e.Source)
            Console.WriteLine("Button_start_stop_hud_LostFocus");
            //button_choose_hud.BorderBrush = LostFocusBrush;
        }

        private void Button_start_stop_hud_GotFocus(object sender, RoutedEventArgs e)
        {

            Console.WriteLine("Button_start_stop_hud_GotFocus");
            //button_choose_hud.BorderBrush = FocusBrush;
        }
    }
}
