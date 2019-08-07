using System;
using System.Runtime.InteropServices; //для DllImport
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;


namespace YL_Player
{
    public partial class DeskBand : Window
    {
        [DllImport("user32", EntryPoint = "SetWindowPos")]
        private static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int wFlags);

        MainWindow mw;
        public Timer timer2 = new Timer();
        public bool isValHover;
        bool isDown, isMove;
        int x, y;

        public Visualization Spectr;

        public DeskBand(MainWindow mw_in)
        {
            mw = mw_in;

            isDown = false;
            isMove = false;
            isValHover = false;
            
            timer2.Interval = 100;
            timer2.Tick += new EventHandler(Timer_Tick);

            InitializeComponent();

            Spectr = new Visualization(this.myGrid, new SpectrParams (){ Opacity = 0.7d });

            timer2.Enabled = true;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            //PresentationSource source = PresentationSource.FromVisual(this);
            //double dpiX, dpiY;
            //dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            //dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            //this.Top = (double)WinForms.Cursor.Position.Y / (dpiY * (1.0 / 96.0)) - (double)490;
            //this.Left = (double)WinForms.Cursor.Position.X / (dpiX * (1.0 / 96.0)) - (double)200;
            //System.Windows.Interop.WindowInteropHelper windowHwnd = new System.Windows.Interop.WindowInteropHelper(this);
            //SetWindowPos(windowHwnd.Handle, -2, (int)((double)this.Left / (dpiX * (1.0 / 96.0))), (int)((double)this.Top / (dpiY * (1.0 / 96.0))), (int)((double)300 / (dpiY * (1.0 / 96.0))), (int)((double)this.Height / (dpiY * (1.0 / 96.0))), 0);
        }

        private void Window_Deactivated_1(object sender, EventArgs e)
        {
            //timer2.Enabled = true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mw.IsActive == false && mw.OwnedWindows.Count == 0)
            {
                System.Windows.Interop.WindowInteropHelper windowHwnd = new System.Windows.Interop.WindowInteropHelper(this);
                SetWindowPos(windowHwnd.Handle, 0, (int)(this.Left * mw.dpiX * 1.0 / 96.0), (int)(this.Top * mw.dpiY * 1.0 / 96.0), (int)(300 * mw.dpiX * 1.0 / 96.0), (int)(this.Height * mw.dpiY * 1.0 / 96.0), 0);
            }
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            timer2.Enabled = false;
        }

        private void Window_LostFocus_1(object sender, RoutedEventArgs e)
        {
            //timer2.Enabled = true;
        }

        public void RDS_UPD(String text)
        {
            mRDS.Content = text;
        }

        private void Prev_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mw.Prev();
        }

        private void Play_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mw.PlayPause();
        }

        private void Pause_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mw.PlayPause();
        }

        private void Next_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mw.Next();
        }

        private void Img_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //mw.Top = 0;
            //mw.Left = 0;
            //mw.Top = (double)WinForms.Cursor.Position.Y / (mw.dpiY * (1.0 / 96.0)) - (double)510;
            //mw.Left = (double)WinForms.Cursor.Position.X / (mw.dpiX * (1.0 / 96.0)) - (double)200;
            if (mw.WindowState == WindowState.Minimized)
                mw.WindowState = WindowState.Normal;
            
            mw.Show();
            mw.Activate();
            /*if (mw.player.pause == false)
            {
                RDS_UPD(" ");
            }
            else
            {
                mw.Search_RDS = mw.RDS_1;
                if (mw.RDS_1.Length > 35) mw.L2 = mw.RDS_1 + "             ";
                else mw.L2 = mw.RDS_1;
                mw.RDS.Content = mw.RDS_1;
                RDS_UPD(mw.RDS_1);
            }*/
        }

        private void VolSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if (mw.IsLoaded == false) return;
            if (!mw.isLoading) mw.CusomVolumeSlider.Value = (float)VolSlider.Value;
            VolL.Content = Convert.ToString(Convert.ToInt32(VolSlider.Value)) + "%";
            //Bass.BASS_ChannelSetAttribute(mw.channel, BASSAttribute.BASS_ATTRIB_VOL, mw.vol / 100);
        }

        private void Hide_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mw.DBShow = 0;
            this.Hide();
            timer2.Enabled = false;
        }

        private void Move_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isMove)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.SizeAll;
                timer2.Enabled = false;
                isDown = true;
                x = Convert.ToInt32((double)WinForms.Cursor.Position.X / (mw.dpiX * (1.0 / 96.0)) - (double)this.Left);
                y = Convert.ToInt32((double)WinForms.Cursor.Position.Y / (mw.dpiY * (1.0 / 96.0)) - (double)this.Top);
            }
        }

        private void Move_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDown && isMove) this.Left = (double)WinForms.Cursor.Position.X / (mw.dpiX * (1.0 / 96.0)) - x;
        }

        private void Move_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isMove)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                timer2.Enabled = true;
                isDown = false;
            }
        }

        private void Move_PreviewMouseLeftButtonUpSwith(object sender, MouseButtonEventArgs e)
        {
            if (isMove) isMove = false;
            else isMove = true;
        }

        /////////////////
        public void DrawPosBar(int pos)
        {
            posBar.Children.Clear();
            Rectangle myRect = new Rectangle();

            Canvas.SetLeft(myRect, 0);
            Canvas.SetBottom(myRect, 0);
            myRect.Height = 3;
            myRect.Width = pos;
            myRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            posBar.Children.Add(myRect);
        }

        private void Window_Activated_1(object sender, EventArgs e)
        {
            timer2.Enabled = true;
        }

        private void PosBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point position = Mouse.GetPosition(this);
            mw.SetCurrentPos((int)position.X);
        }

        private void VolSlider_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            isValHover = true;
            VolL.Content = Convert.ToString(Convert.ToInt32(VolSlider.Value)) + "%";
        }

        private void VolSlider_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            isValHover = false;
        }
        ////////////////
    }
}
