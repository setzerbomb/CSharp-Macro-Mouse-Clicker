﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MouseKeyboardActivityMonitor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Clicker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MouseActionViewModel MouseActions { get; set; }
        public Settings Settings { get; set; }
        MouseHookListener M;

        public MainWindow()
        {
            InitializeComponent();

            MouseActions = new MouseActionViewModel();
            Settings = new Settings();

            if (File.Exists(Settings.SettingsFilename))
            {
                Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Settings.SettingsFilename));
                AutorunCheckbox.IsChecked = Settings.Autorun;
            }

            if (File.Exists(Settings.AutosaveFilename))
            {
                var autoload = File.ReadAllText(Settings.AutosaveFilename);
                var actions = JsonConvert.DeserializeObject<JArray>(autoload);
                foreach (var action in actions)
                {
                    var xPosition = (int)action["XPosition"];
                    var yPosition = (int)action["YPosition"];
                    var cooldown = (TimeSpan)action["Cooldown"];
                    var type = (ActionType)(int)action["Type"];
                    var clickType = (ClickType)(int)action["Button"];
                    var text = (string)action["Text"];
                    MouseActions.Actions.Add(new Action(xPosition, yPosition, cooldown, clickType, type, text));
                }

                if (Settings.Autorun) MouseActions.RunActions();
            }

            MouseKeyboardActivityMonitor.WinApi.GlobalHooker h = new MouseKeyboardActivityMonitor.WinApi.GlobalHooker();

            M = new MouseHookListener(h);

            M.MouseClick += (object s, System.Windows.Forms.MouseEventArgs e) =>
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Middle && MouseActions.IsRunning == false)
                {
                    MouseActions.Actions.Add(new Action(
                e.X,
                e.Y,
                Settings.DefaultCooldown
                ));
                }
            };

            App.Current.Exit += (object sender, ExitEventArgs e) =>
            {
                MouseActions.IsStopRequested = true;
                M?.Stop();

                WriteToJsonFile<Collection<Action>>(Settings.AutosaveFilename, MouseActions.Actions);
                WriteToJsonFile<Settings>(Settings.SettingsFilename, Settings);
            };

            M.Start();
        }

        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        private void ClearButton(object sender, RoutedEventArgs e) { MouseActions.Actions.Clear(); }

        private void RunButton(object sender, RoutedEventArgs e) { MouseActions.RunActions(); }

        private void StopButton(object sender, RoutedEventArgs e) { MouseActions.IsStopRequested = true; }

        private void AutorunCheckbox_Click(object sender, RoutedEventArgs e)
        {
            Settings.Autorun = AutorunCheckbox.IsChecked ?? false;
        }
    }
}
