using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO.Ports;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AtenHdmiSwitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static SerialPort serialPort;
        private AtenData data = new AtenData();
        public AtenData Data {  get { return data; } }
        string receivedData = "";
        bool onScanStartup = true;
        public MainWindow()
        {
            InitializeComponent();
            RefreshMaximizeRestoreButton();
            DataContext = Data;

            serialPort = new SerialPort(SerialPort.GetPortNames()[1], 19200, Parity.None, 8, StopBits.One);
            serialPort.Close();
            serialPort.Open();
            serialPort.DiscardOutBuffer();
            serialPort.DiscardInBuffer();
            serialPort.ReadExisting();
            serialPort.DataReceived += OnScan;

            serialPort.WriteLine("read");
        }
        void UpdateSettingsClick(object sender, RoutedEventArgs e)
        {
            serialPort.WriteLine("read");
        }
        void OnScan(object sender, SerialDataReceivedEventArgs args)
        {
            SerialPort port = sender as SerialPort;
            
            
            receivedData = receivedData + port.ReadExisting();
            port.DiscardInBuffer();

            // This Aten Switch does not send out a clean end signal.. So we have to check for the last bit of information until we parse the settings.
            if (receivedData.Contains("F/W") && receivedData.Contains("V1.1.10"))
            {

                receivedData = receivedData.Replace("\r\r\r", "").Replace("\r\r", "").Replace("\n\n", "").Replace("read", "").Replace("Command OK", "");
                data.SwitchSettingsRaw = receivedData.Substring(receivedData.IndexOf("\r") + 2);
                if(onScanStartup)
                {
                    onScanStartup = false;
                    Data.StartupData = data.SwitchSettingsRaw;
                }
                receivedData = "";
            }
        }

        private void PortSwitchClick(object sender, RoutedEventArgs e)
        {
            string portNumber = ((Button)sender).Tag.ToString();
            int port = int.Parse(portNumber);
            if (port != Data.CurrentPort)
            {
                Console.WriteLine("Port Number selected: " + portNumber);
                serialPort.WriteLine("sw i" + portNumber);
                Data.CurrentPort = port;
            }
        }
        private void OutPortClick(object sender, RoutedEventArgs e)
        {
            string command = ((Button)sender).Tag.ToString();
            if(command == "on" || command == "off")
            {
                Console.WriteLine("Output Port is now: "+command);
                serialPort.WriteLine("sw " + command);
                Data.OutputPortOn = command == "on";
            }
        }
        private void PriorityClick(object sender, RoutedEventArgs e)
        {
            string command = ((Button)sender).Tag.ToString();
            if (command == "next")
            {                
                serialPort.WriteLine("swmode next");
                Data.SwitchMode = SWMode.Next;
            }
            else if(command == "on")
            {
                serialPort.WriteLine("swmode i0"+Data.CurrentPort+" priority");
                Data.SwitchMode = SWMode.Priority;
            }
            else if(command == "off")
            {
                serialPort.WriteLine("swmode off");
                Data.SwitchMode = SWMode.Off;
            }
            Console.WriteLine("Switch Mode is now: " + command);
        }
        private void PodClick(object sender, RoutedEventArgs e)
        {
            string command = ((Button)sender).Tag.ToString();
            if (command == "on" || command == "off")
            {
                serialPort.WriteLine("swmode pod " + command);
                Data.PodModeOn = command == "on";
            }
        }

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            RefreshMaximizeRestoreButton();
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
            RefreshMaximizeRestoreButton();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void RefreshMaximizeRestoreButton()
        {
            if (WindowState == WindowState.Maximized)
            {
                maximizeButton.Visibility = Visibility.Collapsed;
                restoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                maximizeButton.Visibility = Visibility.Visible;
                restoreButton.Visibility = Visibility.Collapsed;
            }
        }
    }
    public enum SWMode
    {
        Next,
        Priority,
        Off
    }

    public enum PODMode
    {
        On,
        Off
    }
    public class AtenData : INotifyPropertyChanged
    {
        private string startupData = "";
        private string switchSettingsRaw = "";
        private int currentPort = 0;
        private bool outputPortOn = true;
        private SWMode switchMode = SWMode.Next;
        private bool podModeOn = true;

        public string SwitchSettingsRaw
        {
            get
            {
                return switchSettingsRaw;
            }
            set
            {
                switchSettingsRaw = value;
                ParseSettings();
                OnPropertyChanged();
            }
        }

        public string StartupData
        {
            get
            {
                return startupData;
            }
            set
            {
                startupData = value;
                OnPropertyChanged();
            }
        }

        public int CurrentPort
        {
            get
            {
                return currentPort;
            }
            set
            {
                currentPort = value;
                OnPropertyChanged();
            }
        }

        public bool OutputPortOn
        {
            get
            {
                return outputPortOn;
            }
            set
            {
                outputPortOn = value;
                OnPropertyChanged();
            }
        }

        public SWMode SwitchMode
        {
            get
            {
                return switchMode;
            }
            set
            {
                switchMode = value;
                OnPropertyChanged();
            }
        }

        public bool PodModeOn
        {
            get
            {
                return podModeOn;
            }
            set
            {
                podModeOn = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }


        void ParseSettings()
        {
            if (switchSettingsRaw.Contains("F/W") && switchSettingsRaw.Contains("V1.1.10"))
            {
                List<string> settings = switchSettingsRaw.Split("\n".ToCharArray()).ToList();
                settings.RemoveAll(s => s == "");
                for (int i = 0; i < settings.Count && i < 4; i++)
                {
                    Console.WriteLine("Setting " + i + ": " + settings[i].Trim());
                    settings[i] = settings[i].Substring(settings[i].IndexOf(":") + 1).Trim();
                    Console.WriteLine("STP: " + settings[i]);
                }

                CurrentPort = ParsePort(settings[0]);
                OutputPortOn = ParseOutPortOn(settings[1]);
                SwitchMode = ParseSwitchMode(settings[2]);
                PodModeOn = ParsePowerOnDetectionMode(settings[3]);
            }
        }
        int ParsePort(string portString)
        {
            Console.WriteLine("Parse Port: "+portString);
            if (int.TryParse(portString.Substring(5), out int number))
                return number;
            else
            {
                return CurrentPort;
            }
        }

        bool ParseOutPortOn(string outportString)
        {
            return outportString.Contains("ON");
        }

        SWMode ParseSwitchMode(string switchModeString)
        {
            switch (switchModeString)
            {
                case "NEXT":
                default:
                    return SWMode.Next;
                case "PRIORITY":
                    return SWMode.Priority;
                case "OFF":
                    return SWMode.Off;
            }
        }

        bool ParsePowerOnDetectionMode(string podString)
        {
            return podString.Contains("ON");
        }
    }
}
