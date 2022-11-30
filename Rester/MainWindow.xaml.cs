using System;
using System.Threading;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Rester
{
    public partial class MainWindow : Window
    {
        // Functions from the dynamic libs
        [DllImport("winAPI.dll", EntryPoint = "SetGrayMoniter", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SetGrayMoniter();

        [DllImport("winAPI.dll", EntryPoint = "ResetMoniter", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr ResetMoniter();

        [DllImport("winAPI.dll", EntryPoint = "GetMoniterStatus", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetMoniterStatus();

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;
        const int WM_WINDOWPOSCHANGING = 0x0046;

        RegistryKey rk = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Rester");

        private double px, py;

        private int StartHour, StartMin, vStartTick, EndHour, EndMin, vEndTick;
        private readonly string[] Days = { "日", "一", "二", "三", "四", "五", "六" };
        private bool[] ActiveDays;
        private Button[] DaysButtons;
        private bool UpdatedApplied = false;

        Brush BrightButton = Brushes.Orange;
        Brush DarkButton = Brushes.Chocolate;

        public MainWindow()
        {
            LoadSettings();
            InitializeComponent(); 
            
            DaysButtons = new Button[Days.Length];

            // Create the button of days
            for (int i = 0; i < Days.Length; i++)
            {
                int day = i;
                DaysButtons[i] = new Button
                {
                    Background = DarkButton,
                    BorderBrush = null,
                    Foreground = Brushes.White,
                    Margin = new Thickness(2, 2, 2, 2),
                    Content = Days[i]
                };
                DaysButtons[i].Click += (sender, e1) => ClickDays(day);
                DaysOfWeekGrid.Children.Add(DaysButtons[i]);
                Grid.SetColumn(DaysButtons[i], i);
            }

            UpdateTextBox();

            // Initialize the loop for checking the time
            Thread main_thread = new Thread(Loop);
            main_thread.Start();

            this.SourceInitialized += new EventHandler(MainWindow_SourceInitialized);;
        }

        /*=================== Hook the windows processing funciton =================*/
        /*====== Reference: https://dotblogs.com.tw/larrynung/2011/12/06/60889 =====*/
        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_WINDOWPOSCHANGING:
                    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);

                    if (px != Left || py != Top)
                    {
                        px = Left;
                        py = Top;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        /*=================== Setting Loading and Saving =================*/
        private void LoadSettings()
        {
            // init the global var
            ActiveDays = new bool[Days.Length];

            // reading start and end time from registry
            StartHour = (int)rk.GetValue("StartHour", 0);
            StartMin = (int)rk.GetValue("StartMin", 0);
            EndHour = (int)rk.GetValue("EndHour", 0);
            EndMin = (int)rk.GetValue("EndMin", 0);

            // reading the startup position
            px = Convert.ToDouble((string)rk.GetValue("LocationX", "0.0"));
            py = Convert.ToDouble((string)rk.GetValue("LocationY", "0.0"));
            this.Top = py;
            this.Left = px;

            // reading activation from bits
            int ActivateBits = (int)rk.GetValue("ActivateDays", 0);
            for (int i = 0; i < Days.Length; i++)
            {
                ActiveDays[i] = (ActivateBits & 1) == 1;
                ActivateBits >>= 1;
            }
        }
        private void SaveSettings()
        {
            // reading start and end time from registry
            rk.SetValue("StartHour", (object)StartHour);
            rk.SetValue("StartMin", (object)StartMin);
            rk.SetValue("EndHour", (object)EndHour);
            rk.SetValue("EndMin", (object)EndMin);

            // reading activation from bits
            int ActivateBits = 0;
            for (int i = Days.Length - 1; i >= 0; i--)
            {
                ActivateBits <<= 1;
                ActivateBits = (ActiveDays[i] ? 1 : 0) | ActivateBits;
            }
            rk.SetValue("ActivateDays", (object)ActivateBits);
        }

        private void SavePosition()
        {
            rk.SetValue("LocationX", (object)px);
            rk.SetValue("LocationY", (object)py);
        }

        /*=================== UI-related Functions =================*/
        private void UpdateTextBox()
        {
            StartHourBox.Text = StartHour.ToString();
            StartMinBox.Text = StartMin.ToString();
            EndHourBox.Text = EndHour.ToString();
            EndMinBox.Text = EndMin.ToString();

            for(int i = 0; i < Days.Length; i++)
                DaysButtons[i].Background = ActiveDays[i] ? BrightButton : DarkButton;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SavePosition();
        }

        private void ClickDays(int day)
        {
            if (ActiveDays[day])
            {
                ActiveDays[day] = false;
                DaysButtons[day].Background = DarkButton;
            }
            else
            {
                ActiveDays[day] = true;
                DaysButtons[day].Background = BrightButton;
            }
            SaveSettings();
            UpdatedApplied = true;
        }

        private void ChangeTime(object sender, TextChangedEventArgs e)
        {
            //update the tick
            vStartTick = StartHour * 60 + StartMin;
            vEndTick = EndHour * 60 + EndMin;

            SaveSettings();
            UpdatedApplied = true;
        }
        private void TimeWheel(object sender, MouseWheelEventArgs e)
        {
            TextBox box = sender as TextBox;
            int delta = e.Delta > 0 ? 1 : -1;

            if (sender == StartHourBox)
            {
                StartHour += delta;
                StartHour = Clamp(StartHour, 0, 24);
                box.Text = StartHour.ToString();
            }
            else if (sender == EndHourBox)
            {
                EndHour += delta;
                EndHour = Clamp(EndHour, 0, 24);
                box.Text = EndHour.ToString();
            }
            else if (sender == StartMinBox)
            {
                StartMin += delta;
                StartMin = Clamp(StartMin, 0, 60);
                box.Text = StartMin.ToString();
            }
            else if (sender == EndMinBox)
            {
                EndMin += delta;
                EndMin = Clamp(EndMin, 0, 60); ;
                box.Text = EndMin.ToString();
            }
        }

        /*=================== Main Loop =================*/
        private void Loop()
        {
            while (true)
            {
                if (IsInRange(DateTime.Now)) 
                    SetGrayMoniter();
                else
                    ResetMoniter();
                
                // constantly wait for 10 sec or updated, could be better to wait until next moniter switch
                SpinWait.SpinUntil(() => UpdatedApplied, 10000);
                UpdatedApplied = false;
            }
        }

        /*=================== Utility Functions =================*/
        private int Clamp(int x, int min, int max)
        {
            int n = (x-min) % (max-min);
            return n < min ? max-1 : n;
        }

        private bool IsInRange(DateTime now)
        {
            bool active_today = ActiveDays[(int)now.DayOfWeek];
            bool active_yesterday = ActiveDays[((int)now.DayOfWeek + 6) % 7];

            // No day corssing
            if (StartTick() <= EndTick())   
                return active_today && NowTick() <= EndTick() && NowTick() >= StartTick();

            // Day crossing
            else
                return 
                    (active_yesterday && NowTick() <= EndTick()) ||
                    (active_today && NowTick() >= StartTick());
        }

        private int NowTick()
        {
            return DateTime.Now.Hour * 60 + DateTime.Now.Minute;
        }

        private int StartTick()
        {
            return vStartTick;
        }

        private int EndTick()
        {
            return vEndTick;
        }
    }
}
