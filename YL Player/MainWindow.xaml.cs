using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Forms;
using WinForms = System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace YL_Player
{
    public class ListType //класс элемента ListBox
    {
        public int index { get; set; }//index
        public String text { get; set; }//название
        public ImageSource image { get; set; }//изображение
        public String duration { get; set; }//длительность
    }

    public partial class MainWindow : Window
    {
        private static MainWindow mw; //текущий класс
        public DeskBand DeskBandPanel;
        public NotifyIcon nIcon = new NotifyIcon();
        public PresentationSource source;
        public double dpiX, dpiY, PixelSize;
        public int DBShow, isVisual;
        public Color VColor;
        public Player player = new Player(NextSong, ShowDuration, OnFileNotFound);   //инициализируем плеер
        public bool isPlayOnStart = false;     //играть при запуске программы (если плейлист не пуст)
        public bool isPlayListLoading = false; //загрузка плейлиста
        public bool isPlayFromStart = true;    //играть трек с начала при нажатии на кнопку PlayPause
        public bool repeat = false; //повторять оду и ту же песню
        public bool tick = false; //флаг равен true во время выполнения функции ShowDuration
        public float muteVol = 100;//переменная, в которой запоминается громкость до нажатия кнопки mute
        public Timer SpectrumTimer = new Timer();
        public ObservableCollection<ListType> PlayList = new ObservableCollection<ListType>();
        public int selectedIndex = -1;
        public bool restoringOfSelectedIndex = false;
        public bool selectedIndexChanged = false;
        public bool isLoading = true;
        public Visualization Spectr;

        public MainWindow()
        {
            InitializeComponent();

            //задать иконку в трее
            nIcon.Icon = System.Drawing.Icon.FromHandle(Properties.Resources.Icon.ToBitmap().GetHicon());
            nIcon.Text = "YL Player";
            nIcon.MouseClick += new WinForms.MouseEventHandler(TrayIcon_MouseClick);
            nIcon.MouseDoubleClick += new WinForms.MouseEventHandler(TrayIcon_MouseDoubleClick);
            
            SpectrumTimer.Interval = 75;
            SpectrumTimer.Tick += new EventHandler(SpectrumTimer_Tick);
            SpectrumTimer.Enabled = true;
            TrackName.Content = "";

            DeskBandPanel = new DeskBand(this);

            Spectr = new Visualization(GridVisualisation, new SpectrParams(40, 512, 10, 0.63f, 3, 15));

            LoadSettings(); //загрузка настроек

            nIcon.Visible = true;
            mw = this;
            isLoading = false;
        }

        private void TrayIcon_MouseClick(object sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Right)
            {/*
                this.Top = 0;
                this.Left = 0;
                this.Top = (double)WinForms.Cursor.Position.Y / (dpiY * (1.0 / 96.0)) - (double)490;
                this.Left = (double)WinForms.Cursor.Position.X / (dpiX * (1.0 / 96.0)) - (double)200;*/
                if (this.WindowState == WindowState.Minimized)
                    this.WindowState = WindowState.Normal;
                    
                this.Show();
                this.Activate();
                /*if (pause == 0)
                {
                    RDS.Content = " ";
                    DeskBandPanel.RDS_UPD(" ");
                    DoMeta();//Обновление мета-тега при открытии меню
                }
                else
                {
                    Search_RDS = RDS_1;
                    if (RDS_1.Length > 32) L2 = RDS_1 + "             ";
                    else L2 = RDS_1;
                    RDS.Content = RDS_1;
                    DeskBandPanel.RDS_UPD(RDS_1);
                }*/
            }
            if (e.Button == WinForms.MouseButtons.Middle)
            {
                this.Close();
            }
        } //Клик по трею
        private void TrayIcon_MouseDoubleClick(object sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Left) PlayPause();
        }

        //Событие - сменился тест в поисковой строке
        private void txtNameToSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlayListBox.ItemsSource = PlayList.Where(x => x.text.ToUpper().Contains(txtNameToSearch.Text.ToUpper()));
            if (txtNameToSearch.Text == "" && !selectedIndexChanged)
            {
                restoringOfSelectedIndex = true;
                PlayListBox.SelectedIndex = selectedIndex;
            }
            else
                selectedIndexChanged = false;
        }

        /// <summary>
        /// Загрузка настроек
        /// </summary>
        void LoadSettings()
        {
            //Настройки по умолчанию
            //Применить тему (Default: Dark Red Chroma)
            this.Resources["TitleBarBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0F0F0"));
            this.Resources["WindowBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2333333"));
            this.Resources["WindowBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
            this.Resources["WindowBorderBrush70"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B2FF0000"));
            this.Resources["WindowBorderBrush30"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CFF0000"));
            this.Resources["WindowBorderBrushInactive"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF999999"));
            this.Resources["WindowStatusForeground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            this.Resources["WindowStatusForegroundInactive"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            //
            DBShow = 1;
            DeskBandPanel.Left = System.Windows.SystemParameters.PrimaryScreenWidth / 2 - 150;
            DeskBandPanel.Top = System.Windows.SystemParameters.PrimaryScreenHeight - 38;
            VColor = Color.FromArgb(255, 255, 0, 0);
            //
            int SelectedIndex = -1, Pos = -1;

            RegistryKey rk = null;
            rk = Registry.CurrentUser.OpenSubKey("Software");
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            RegistryKey nk = rk.OpenSubKey(versionInfo.ProductName + "_" + versionInfo.ProductVersion);
            if (rk != null)
            {
                if (nk != null)
                {
                    this.Left = Convert.ToDouble(nk.GetValue("MWLeft").ToString());
                    this.Top = Convert.ToDouble(nk.GetValue("MWTop").ToString());
                    this.Width = Convert.ToDouble(nk.GetValue("MWWidth").ToString());
                    this.Height = Convert.ToDouble(nk.GetValue("MWHeight").ToString());
                    this.WindowState = (nk.GetValue("MWWindowState").ToString() == "Normal") ? WindowState.Normal : (nk.GetValue("MWWindowState").ToString() == "Minimized") ? WindowState.Minimized : WindowState.Maximized;
                    this.Visibility = (nk.GetValue("MWVisibility").ToString() == "Hidden") ? Visibility.Hidden : Visibility.Visible;

                    if (this.WindowState == System.Windows.WindowState.Maximized)
                    {
                        MaxButton.Visibility = Visibility.Hidden;
                        NormButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        NormButton.Visibility = Visibility.Hidden;
                        MaxButton.Visibility = Visibility.Visible;
                    }

                    //предустановка эквалайзера
                    float[] EQpreset = new float[18];
                    for (int i = 0; i < 18; i++)
                    {
                        EQpreset[i] = (float)Convert.ToDouble(nk.GetValue("EQ_FX_" + i).ToString()) / 10f;
                        player.UpdateEQ(i, EQpreset[i] / 10f);
                    }
                    player.SetEQ(EQpreset);
                    fx1.Value = EQpreset[0] * 10f;
                    fx2.Value = EQpreset[1] * 10f;
                    fx3.Value = EQpreset[2] * 10f;
                    fx4.Value = EQpreset[3] * 10f;
                    fx5.Value = EQpreset[4] * 10f;
                    fx6.Value = EQpreset[5] * 10f;
                    fx7.Value = EQpreset[6] * 10f;
                    fx8.Value = EQpreset[7] * 10f;
                    fx9.Value = EQpreset[8] * 10f;
                    fx10.Value = EQpreset[9] * 10f;
                    fx11.Value = EQpreset[10] * 10f;
                    fx12.Value = EQpreset[11] * 10f;
                    fx13.Value = EQpreset[12] * 10f;
                    fx14.Value = EQpreset[13] * 10f;
                    fx15.Value = EQpreset[14] * 10f;
                    fx16.Value = EQpreset[15] * 10f;
                    fx17.Value = EQpreset[16] * 10f;
                    fx18.Value = EQpreset[17] * 10f;
                    //баланс
                    BalanceSlider.Value = Convert.ToDouble(nk.GetValue("BALANCE_FX").ToString());
                    CusomVolumeSlider.Value = Convert.ToDouble(nk.GetValue("VOLUME").ToString());
                    DeskBandPanel.VolSlider.Value = CusomVolumeSlider.Value;

                    SelectedIndex = Convert.ToInt32(nk.GetValue("SelectedIndex").ToString()); //Индекс трека

                    //Pos = Convert.ToInt32(nk.GetValue("Pos").ToString()); // TODO : CusomSlider.Value = Pos >= 0 ? Pos : 0;
                    //player.SetPosOfScroll(Pos);
                    
                    isPlayOnStart = Convert.ToBoolean(nk.GetValue("PlayOnStart").ToString()); //Воспроизводить ли при старте

                    isVisual = Convert.ToInt32(nk.GetValue("Visual").ToString()); //визуализация
                    VColor = (Color)ColorConverter.ConvertFromString(nk.GetValue("VColor").ToString());

                    DBShow = Convert.ToInt32(nk.GetValue("DBShow").ToString()); //Показывать мини-панель управления
                    DeskBandPanel.Left = Convert.ToInt32(nk.GetValue("DBX").ToString());
                    DeskBandPanel.Top = Convert.ToInt32(nk.GetValue("DBY").ToString());
                    /*if (CusomVolumeSlider.Value >= 0.0) DeskBandPanel.VolSlider.Value = CusomVolumeSlider.Value;
                    else DeskBandPanel.VolSlider.Value = 100;*/
                    if (DBShow == 1)
                        DeskBandPanel.Show();

                    //Применить тему (Default: Dark Red Chroma)
                    this.Resources["TitleBarBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF0F0F0"));
                    this.Resources["WindowBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));
                    this.Resources["WindowBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
                    this.Resources["WindowBorderBrush70"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B2FF0000"));
                    this.Resources["WindowBorderBrush30"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CFF0000"));
                    this.Resources["WindowBorderBrushInactive"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF999999"));
                    this.Resources["WindowStatusForeground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
                    this.Resources["WindowStatusForegroundInactive"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
                    //

                    //Применить тему (Default: Light Red Chroma)
                    /*this.Resources["TitleBarBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));
                    this.Resources["WindowBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2ECECEC"));
                    this.Resources["WindowBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
                    this.Resources["WindowBorderBrush70"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B2FF0000"));
                    this.Resources["WindowBorderBrush30"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CFF0000"));
                    this.Resources["WindowBorderBrushInactive"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF999999"));
                    this.Resources["WindowStatusForeground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));
                    this.Resources["WindowStatusForegroundInactive"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333"));*/

                    nk.Close();
                }
                rk.Close();
            }

            if (System.IO.File.Exists("default.m3u8")) //если плейлист существует
            {
                String[] TMPstr = new String[1];
                TMPstr[0] = "default.m3u8";
                player.PlaylistUpdate(TMPstr, MODE.ADD);//Загрузка плейлиста по умолчанию
                LoadPlayList();//Загрузка плейлиста

                if (SelectedIndex != -1 && PlayListBox.Items.Count > SelectedIndex)
                    PlayListBox.SelectedIndex = SelectedIndex;
            }
        }
        /// <summary>
        /// Загрузка плейлиста
        /// </summary>
        void LoadPlayList()
        {
            int si = PlayListBox.SelectedIndex;
            isPlayListLoading = true;
            PlayList.Clear();
            ListType[] CH_EL = new ListType[player.PlayList.Count];
            for (int i = 0; i < player.PlayList.Count; i++)
            {
                CH_EL[i] = new ListType();
                CH_EL[i].index = i;
                CH_EL[i].text = ((player.PlayList[i].artist != null && player.PlayList[i].artist.Length > 0) ? player.PlayList[i].artist + " - " : "") +
                                ((player.PlayList[i].title != null && player.PlayList[i].title.Length > 0) ? player.PlayList[i].title :
                                player.PlayList[i].path.Substring(player.PlayList[i].path.LastIndexOf('\\') + 1, player.PlayList[i].path.Length - player.PlayList[i].path.LastIndexOf('\\') - 5));
                if (player.PlayList[i].image != null)
                {
                    var bitmap = new System.Drawing.Bitmap(player.PlayList[i].image);
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),
                                          IntPtr.Zero,
                                          Int32Rect.Empty,
                                          BitmapSizeOptions.FromEmptyOptions());
                    CH_EL[i].image = bitmapSource;
                }
                else
                    CH_EL[i].image = DefaultAlbumArtSmall.Source;
                CH_EL[i].duration = SecToString(player.PlayList[i].duration);
            }
            for (int i = 0; i < player.PlayList.Count; i++)
                PlayList.Add(CH_EL[i]);//PlayListBox.Items.Add(CH_EL[i]);
            PlayListBox.ItemsSource = PlayList;
            PlayListBox.Items.Refresh();
            PlayListBox.UpdateLayout();
            if (si == -1) PlayListBox.SelectedIndex = 0;
            else PlayListBox.SelectedIndex = si;
            PlayListBox.Focus();
            CusomSlider.Value = 0;
        }
        /// <summary>
        /// Отобразить текущее время воспроизведения
        /// </summary>
        /// <param name="duration"></param>
        public static void ShowDuration(int duration)
        {
            mw.tick = true;
            String tmp = mw.player.DoMeta(); //пробуем прочитать мета данные потока
            if (tmp != "") // если удалось, то
                mw.TrackName.Content = tmp; //указываем название трека
            mw.CurDur.Content = mw.SecToString(duration); //отобразить текущее время воспроизведения
            if (mw.player.GetDurationOfStream() > 0)
                mw.DeskBandPanel.DrawPosBar(duration * 300 / mw.player.GetDurationOfStream());
            mw.CusomSlider.Value = duration;//передвинуть ползунок воспроизведения
            mw.tick = false;
        }
        /// <summary>
        /// Переключить на следующий трек
        /// </summary>
        public static void NextSong()
        {
            //если повторение трека включено, то повторяем, иначе переключаем трек
            if (mw.repeat) { mw.isPlayFromStart = true; mw.PlayPause(); return; }
            if (mw.PlayListBox.SelectedIndex < mw.PlayListBox.Items.Count - 1)
                mw.PlayListBox.SelectedIndex++;
            else
                mw.PlayListBox.SelectedIndex = 0;
        }
        /// <summary>
        /// Событие при отсутствии файла
        /// </summary>
        /// <param name="index"></param>
        public static void OnFileNotFound(int index)
        {   //сообщаем, об ошибке
            System.Windows.MessageBox.Show("Файл не найден", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            mw.RemoveTrack(index);//удалить отсутствующий трек
        }
        /// <summary>
        /// Представить время в секундах (например, 5) в виде 0:05
        /// </summary>
        /// <param name="sec"></param>
        /// <returns></returns>
        private String SecToString(int sec)
        {   //если есть нормальное время, то вернуть в формате 0:00, иначе это поток, поэтому вернуть пустую строку
            return (sec != -1) ? Convert.ToString(sec / 60) + ":" + (sec - (sec / 60) * 60).ToString("D2") : "";
        }
        private void UpdateAlbumArt(System.Drawing.Image img)
        {
            if (img != null)
            {
                var bitmap = new System.Drawing.Bitmap(/*player.PlayList[PlayListBox.SelectedIndex].image*/img);
                var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(),
                                      IntPtr.Zero,
                                      Int32Rect.Empty,
                                      BitmapSizeOptions.FromEmptyOptions());
                AlbumArt.Background = new ImageBrush(bitmapSource);
                AlbumArt.Visibility = System.Windows.Visibility.Visible;
                DeskBandPanel.img.Source = bitmapSource;
                
            }
            else
            {
                DeskBandPanel.img.Source = DefaultAlbumArtSmall.Source;
                AlbumArt.Visibility = System.Windows.Visibility.Hidden;   
            }
        }
        /// <summary>
        /// Воспроизвести/пауза
        /// </summary>
        public void PlayPause()
        {
            if (player.PlayList.Count == 0 || PlayListBox.SelectedIndex < 0) return;

            if (PlayListBox.SelectedIndex >= 0)
                UpdateAlbumArt(player.PlayList[(PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index].image);
            //меняем кнопки плей и пауза
            if (PlayBut.Visibility == System.Windows.Visibility.Hidden || PlayListBox.SelectedIndex == -1)
            {
                PlayBut.Visibility = System.Windows.Visibility.Visible;
                PauseBut.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                PlayBut.Visibility = System.Windows.Visibility.Hidden;
                PauseBut.Visibility = System.Windows.Visibility.Visible;
            }
            if (isPlayFromStart || isPlayListLoading)//если играть при старте программы
            {
                CusomSlider.Maximum = player.PlayList[PlayListBox.SelectedIndex].duration;//меняем диаппазон ползунка
                Dur.Content = SecToString(player.PlayList[PlayListBox.SelectedIndex].duration);//указываем длительность трека
                player.Play((PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index);//воспроизвести
                isPlayFromStart = false;//отключаем флаг
                return;
            }
            CusomSlider.Maximum = player.PlayList[PlayListBox.SelectedIndex].duration; //меняем диаппазон ползунка
            Dur.Content = SecToString(player.PlayList[PlayListBox.SelectedIndex].duration);//указываем длительность трека
            player.PlayPause((PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index);//воспроизвести или поставить на паузу текущий трек
        }
        /// <summary>
        /// Нажатие кнопки PlayPause
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayBut_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PlayPause();
        }
        public void Next()
        {
            if (player.PlayList.Count > 0)
            {
                if ((PlayList[selectedIndex] as ListType).index < PlayList.Count - 1)
                    selectedIndex = (PlayList[selectedIndex] as ListType).index + 1;
                else
                    selectedIndex = 0;
                selectedIndexChanged = true;
                if (txtNameToSearch.Text == "")
                {
                    restoringOfSelectedIndex = true;
                    PlayListBox.SelectedIndex = selectedIndex;
                }
                UpdateAlbumArt(player.PlayList[(PlayList[selectedIndex] as ListType).index].image);
                Dur.Content = SecToString(player.PlayList[selectedIndex].duration);
                TrackName.Content = ((ListType)PlayList[selectedIndex]).text;
                DeskBandPanel.RDS_UPD(((ListType)PlayList[selectedIndex]).text);
                if (!isPlayOnStart && isPlayListLoading)//если не играть при запуске программы то ничего не воспроизводим
                    isPlayListLoading = false;
                else
                {
                    CusomSlider.Maximum = player.PlayList[selectedIndex].duration;
                    CusomSlider.Value = 0;
                    player.Play((PlayList[selectedIndex] as ListType).index);//воспроизвести выбранный трек
                    PlayBut.Visibility = System.Windows.Visibility.Hidden;
                    PauseBut.Visibility = System.Windows.Visibility.Visible;
                    isPlayFromStart = false;
                }
                /*UpdateAlbumArt(player.PlayList[(PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index].image);
                if ((PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index < PlayList.Count - 1)
                    PlayListBox.SelectedIndex = (PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index + 1;
                else
                    PlayListBox.SelectedIndex = 0;*/
            }
        }
        /// <summary>
        /// Переключить на следующий трек
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NextBut_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Next();
        }
        public void Prev()
        {
            if (player.PlayList.Count > 0)
            {
                if ((PlayList[selectedIndex] as ListType).index > 0)
                    selectedIndex = (PlayList[selectedIndex] as ListType).index - 1;
                else
                    selectedIndex = PlayList.Count - 1;
                selectedIndexChanged = true;
                if (txtNameToSearch.Text == "")
                {
                    restoringOfSelectedIndex = true;
                    PlayListBox.SelectedIndex = selectedIndex;
                }
                UpdateAlbumArt(player.PlayList[(PlayList[selectedIndex] as ListType).index].image);
                Dur.Content = SecToString(player.PlayList[selectedIndex].duration);
                TrackName.Content = ((ListType)PlayList[selectedIndex]).text;
                DeskBandPanel.RDS_UPD(((ListType)PlayList[selectedIndex]).text);
                if (!isPlayOnStart && isPlayListLoading)//если не играть при запуске программы то ничего не воспроизводим
                    isPlayListLoading = false;
                else
                {
                    CusomSlider.Maximum = player.PlayList[selectedIndex].duration;
                    CusomSlider.Value = 0;
                    player.Play((PlayList[selectedIndex] as ListType).index);//воспроизвести выбранный трек
                    PlayBut.Visibility = System.Windows.Visibility.Hidden;
                    PauseBut.Visibility = System.Windows.Visibility.Visible;
                    isPlayFromStart = false;
                }
            }
            /*UpdateAlbumArt(player.PlayList[(PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index].image);
            if ((PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index > 0)
                PlayListBox.SelectedIndex = (PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index - 1;
            else
                PlayListBox.SelectedIndex = PlayList.Count - 1;*/
        }
        /// <summary>
        /// Переключить на предыдущий трек
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrevBut_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Prev();
        }
        /// <summary>
        /// Добавить треки или плейлист
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Add_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((BlurEffect)GridMain.Effect).Radius = 15;
            GridWait.Visibility = System.Windows.Visibility.Visible;
            LoadingText.Content = "Загрузка файлов";
            Microsoft.Win32.OpenFileDialog myDialog = new Microsoft.Win32.OpenFileDialog();
            myDialog.Filter = "Audio File or Playlist (*.mp3;*.wav;*.ogg;*.flac;*.m3u;*.m3u8)|*.mp3;*.wav;*.ogg;*.flac;*.m3u;*.m3u8;";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = true;
            if (myDialog.ShowDialog() == true)//если были выбраны файлы
            {
                //создаем список для дальнейшей группировки
                List<string[]> list = new List<string[]>();
                int N = 0;
                //остсеиваем папки от файлов
                for (int i = 0; i < myDialog.FileNames.Length; i++)
                {
                    if ((File.GetAttributes(myDialog.FileNames[i]) & FileAttributes.Directory) == FileAttributes.Directory) //если папка
                    {
                        list.Add(GetFilesFromDirectory(myDialog.FileNames[i])); //получаем список файлов в этой папке и подпапках
                        N += list[i].Length;
                    }
                    else //иначе файл
                    {
                        list.Add(new string[] { myDialog.FileNames[i] });
                        N++;
                    }
                }
                //создаем общий массив
                string[] FileNames = new string[N];
                //группируем все в один массив
                for (int i = 0, k = 0; i < list.Count; i++)
                {
                    for (int j = 0; j < list[i].Length; j++)
                    {
                        FileNames[k] = list[i][j];
                        k++;
                    }
                }

                //добавляем треки или плейлист в текущий плейлист
                player.PlaylistUpdate(FileNames, MODE.ADD);
                LoadPlayList();//обновляем плейлист интерфейса
                player.SavePlayListToFile_m3u8("default.m3u8");//сохраняем плейлист по умолчанию
            }
            GridWait.Visibility = System.Windows.Visibility.Hidden;
            ((BlurEffect)GridMain.Effect).Radius = 0;
        }
        /// <summary>
        /// Сохранить плейлист как
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveBut_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((BlurEffect)GridMain.Effect).Radius = 15;
            GridWait.Visibility = System.Windows.Visibility.Visible;
            LoadingText.Content = "Сохранение плейлиста";
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Playlist"; // Default file name
            dlg.DefaultExt = ".m3u8";
            dlg.Filter = "Playlists (.m3u8)|*.m3u8";
            if (dlg.ShowDialog() == true)
                player.SavePlayListToFile_m3u8(dlg.FileName);//сохраняем плейлист как
            GridWait.Visibility = System.Windows.Visibility.Hidden;
            ((BlurEffect)GridMain.Effect).Radius = 0;
        }
        /// <summary>
        /// Удалить текущий трек из плейлиста
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Remove_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RemoveTrack(PlayListBox.SelectedIndex);//удалить текущий трек из плейлиста
        }
        /// <summary>
        /// Выход из программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //save settings
            //открытие ветки реестра
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("Software", true);
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            RegistryKey nk = rk.CreateSubKey(versionInfo.ProductName + "_" + versionInfo.ProductVersion);

            nk.SetValue("MWLeft", Convert.ToString(this.Left));
            nk.SetValue("MWTop", Convert.ToString(this.Top));
            nk.SetValue("MWWidth", Convert.ToString(this.Width));
            nk.SetValue("MWHeight", Convert.ToString(this.Height));
            nk.SetValue("MWWindowState", Convert.ToString(this.WindowState.ToString()));
            nk.SetValue("MWVisibility", Convert.ToString(this.Visibility.ToString()));
            
            nk.SetValue("EQ_FX_0", Convert.ToString(fx1.Value));
            nk.SetValue("EQ_FX_1", Convert.ToString(fx2.Value));
            nk.SetValue("EQ_FX_2", Convert.ToString(fx3.Value));
            nk.SetValue("EQ_FX_3", Convert.ToString(fx4.Value));
            nk.SetValue("EQ_FX_4", Convert.ToString(fx5.Value));
            nk.SetValue("EQ_FX_5", Convert.ToString(fx6.Value));
            nk.SetValue("EQ_FX_6", Convert.ToString(fx7.Value));
            nk.SetValue("EQ_FX_7", Convert.ToString(fx8.Value));
            nk.SetValue("EQ_FX_8", Convert.ToString(fx9.Value));
            nk.SetValue("EQ_FX_9", Convert.ToString(fx10.Value));
            nk.SetValue("EQ_FX_10", Convert.ToString(fx11.Value));
            nk.SetValue("EQ_FX_11", Convert.ToString(fx12.Value));
            nk.SetValue("EQ_FX_12", Convert.ToString(fx13.Value));
            nk.SetValue("EQ_FX_13", Convert.ToString(fx14.Value));
            nk.SetValue("EQ_FX_14", Convert.ToString(fx15.Value));
            nk.SetValue("EQ_FX_15", Convert.ToString(fx16.Value));
            nk.SetValue("EQ_FX_16", Convert.ToString(fx17.Value));
            nk.SetValue("EQ_FX_17", Convert.ToString(fx18.Value));
            nk.SetValue("BALANCE_FX", Convert.ToString(BalanceSlider.Value));
            nk.SetValue("VOLUME", Convert.ToString(CusomVolumeSlider.Value));
            nk.SetValue("SelectedIndex", (PlayListBox.Items.Count > 0) ? Convert.ToString(PlayListBox.SelectedIndex) : "-1"); //Запоминаем текущий трек
            nk.SetValue("Pos", (PlayListBox.Items.Count > 0) ? Convert.ToString(player.GetCurrentPlayingPos()) : "0");
            nk.SetValue("PlayOnStart", Convert.ToString(isPlayOnStart));

            //визуализация
            nk.SetValue("Visual", Convert.ToString(isVisual));
            nk.SetValue("VColor", ColorToHex(VColor));

            //Показывать мини-панель управления
            nk.SetValue("DBShow", Convert.ToString(DBShow));
            nk.SetValue("DBX", Convert.ToString((int)DeskBandPanel.Left));
            nk.SetValue("DBY", Convert.ToString((int)DeskBandPanel.Top));
            

            //закрытие ветки реестра
            nk.Close();
            rk.Close();

            player.SavePlayListToFile_m3u8("default.m3u8");//сохраняем плейлист по умолчанию
            player.Stop();//остановить воспроизведение
            this.nIcon.Visible = false;
            DeskBandPanel.Close();
        }
        public string ColorToHex(Color color)
        {
            return String.Format("#{0}{1}{2}{3}"
                , color.A.ToString("X").Length == 1 ? String.Format("0{0}", color.A.ToString("X")) : color.A.ToString("X")
                , color.R.ToString("X").Length == 1 ? String.Format("0{0}", color.R.ToString("X")) : color.R.ToString("X")
                , color.G.ToString("X").Length == 1 ? String.Format("0{0}", color.G.ToString("X")) : color.G.ToString("X")
                , color.B.ToString("X").Length == 1 ? String.Format("0{0}", color.B.ToString("X")) : color.B.ToString("X"));
        }
        /// <summary>
        /// Удалить трек с индексом index из плейлиста
        /// </summary>
        /// <param name="index"></param>
        private void RemoveTrack(int index)
        {
            if (index >= 0 && index < player.PlayList.Count)
            {
                AlbumArt.Visibility = System.Windows.Visibility.Hidden;
                player.Stop();
                player.PlayList.RemoveAt(index);
                PlayList.RemoveAt(index);
                TrackName.Content = "";
                CurDur.Content = SecToString(0);
                Dur.Content = SecToString(0);
                if (PlayListBox.Items.Count > 0)
                    PlayListBox.SelectedIndex = index - 1;
                player.SavePlayListToFile_m3u8("default.m3u8");
            }
        }
        /// <summary>
        /// Изменение выбора текущего трека (переключение трека)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (restoringOfSelectedIndex)
                restoringOfSelectedIndex = false;
            else
            {
                if (PlayListBox.SelectedIndex >= 0 && PlayListBox.SelectedIndex < player.PlayList.Count)
                {
                    selectedIndex = (PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index;
                    selectedIndexChanged = true;
                    UpdateAlbumArt(player.PlayList[(PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index].image);
                    Dur.Content = SecToString(player.PlayList[PlayListBox.SelectedIndex].duration);
                    TrackName.Content = ((ListType)PlayListBox.Items[PlayListBox.SelectedIndex]).text;
                    DeskBandPanel.RDS_UPD(((ListType)PlayListBox.Items[PlayListBox.SelectedIndex]).text);
                    if (!isPlayOnStart && isPlayListLoading)//если не играть при запуске программы то ничего не воспроизводим
                        isPlayListLoading = false;
                    else
                    {
                        CusomSlider.Maximum = player.PlayList[PlayListBox.SelectedIndex].duration;
                        CusomSlider.Value = 0;
                        if (isLoading && !isPlayOnStart) return;
                        player.Play((PlayListBox.Items[PlayListBox.SelectedIndex] as ListType).index);//воспроизвести выбранный трек
                        PlayBut.Visibility = System.Windows.Visibility.Hidden;
                        PauseBut.Visibility = System.Windows.Visibility.Visible;
                        isPlayFromStart = false;
                    }
                }
            }
        }
        /// <summary>
        /// Вкл/выкл повторения трека
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepeatBut_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!repeat)
            {
                repeat = true;
                NoRepeatBut.Visibility = System.Windows.Visibility.Hidden;
                RepeatBut.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                repeat = false;
                NoRepeatBut.Visibility = System.Windows.Visibility.Visible;
                RepeatBut.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        /// <summary>
        /// Очистить плейлист
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearBut_MouseUp(object sender, MouseButtonEventArgs e)
        {
            player.Stop();//останавливаем воспроизведение
            PlayBut.Visibility = System.Windows.Visibility.Visible;
            PauseBut.Visibility = System.Windows.Visibility.Hidden;
            AlbumArt.Visibility = System.Windows.Visibility.Hidden;
            TrackName.Content = "";
            CurDur.Content = SecToString(0);
            Dur.Content = SecToString(0);
            PlayList.Clear();//очищаем плейлист интерфейса
            player.PlayList.Clear();//очищаем плейлист
            player.SavePlayListToFile_m3u8("default.m3u8");//сохраняем изменения
        }
        /// <summary>
        /// Изменение громкости
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CusomSliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if (mw == null || mw.CusomVolumeSlider == null) return;
            if (DeskBandPanel != null)
                DeskBandPanel.VolSlider.Value = CusomVolumeSlider.Value;
            SetVolume();
        }
        public void SetVolume()
        {
            if (VolLabel != null) VolLabel.Content = (int)CusomVolumeSlider.Value + "%";
            player.SetVolume((float)CusomVolumeSlider.Value * 0.01f);//задаем громкость
        }
        /// <summary>
        /// Изменение времени текущего воспроизведения (перемотка)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CusomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetCurrentPos((int)CusomSlider.Value);
        }
        public void SetCurrentPos(int value)
        {
            if (tick) return;//если ползунок сдвинулся программой, то ничего не делать
            CurDur.Content = mw.SecToString(value);//иначе задаем время воспроизведения
            player.SetPosOfScroll(value);//перематываем на указанное время
        }
        /// <summary>
        /// Кнопка отключения звука
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MuteBut_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (player.volume != 0)//если громкость > 0  то заглушить звук
            {
                MuteBut.Visibility = System.Windows.Visibility.Hidden;
                MuteButOn.Visibility = System.Windows.Visibility.Visible;
                muteVol = player.volume;
                CusomVolumeSlider.Value = 0;
            }
            else//иначе вернуть звук обратно
            {
                MuteBut.Visibility = System.Windows.Visibility.Visible;
                MuteButOn.Visibility = System.Windows.Visibility.Hidden;
                CusomVolumeSlider.Value = muteVol * 100;
            }
        }
        
        /// <summary>
        /// Обновление визуализации по тику
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpectrumTimer_Tick(object sender, EventArgs e)
        {
            Spectr.Spectrum(player.GetFFTData());
            DeskBandPanel.Spectr.Spectrum(player.GetFFTData());
            String tmp = player.DoMeta(); //пробуем прочитать мета данные потока
            if (tmp != "") // если удалось, то
                DeskBandPanel.RDS_UPD(tmp);//указываем название трека
        }
        /// <summary>
        /// Отключение визуализации
        /// </summary>
        public void SpectrTimerOff()
        {
            SpectrumTimer.Enabled = false;
            GridVisualisation.Children.Clear();
        }

        private void EQ_fx_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider FX = sender as Slider;
            player.UpdateEQ(Convert.ToInt32(FX.Name.Substring(2)) - 1, (float)FX.Value / 10f);
            float fxValue = (float)FX.Value / 10f;
            FX.ToolTip = ((fxValue >= 0) ? "+" : "") + fxValue.ToString("F1") + "dB";
        }

        private void FX_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Slider FX = sender as Slider;
            FX.Value = 0;
        }

        private void CloseEQ_MouseUp(object sender, MouseButtonEventArgs e)
        {
            GridEQ.Visibility = System.Windows.Visibility.Hidden;
            ((BlurEffect)GridMain.Effect).Radius = 0;
        }

        private void OpenEQBut_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((BlurEffect)GridMain.Effect).Radius = 15;
            GridEQ.Visibility = System.Windows.Visibility.Visible;
        }

        private void BalanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider Balance = sender as Slider;
            player.SetBalance((float)Balance.Value / 100f);
            Balance.ToolTip = "Баланс (" + ((Balance.Value < 0) ? "L " + Balance.Value.ToString("F1") : ((Balance.Value > 0) ? "R +" + Balance.Value.ToString("F1") : "L = R")) + ")";
        }
        /// <summary>
        /// составить список файлов форматов flac mp3 wav ogg m3u m3u8,
        /// содаржащихся в папке по указонному пути, а также во всех подпапках 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string[] GetFilesFromDirectory(string path)
        {
            List<string[]> list = new List<string[]>();
            list.Add(Directory.GetFiles(path, "*?.flac", SearchOption.AllDirectories));
            list.Add(Directory.GetFiles(path, "*?.mp3", SearchOption.AllDirectories));
            list.Add(Directory.GetFiles(path, "*?.wav", SearchOption.AllDirectories));
            list.Add(Directory.GetFiles(path, "*?.ogg", SearchOption.AllDirectories));
            list.Add(Directory.GetFiles(path, "*?.m3u", SearchOption.AllDirectories));
            list.Add(Directory.GetFiles(path, "*?.m3u8", SearchOption.AllDirectories));
            //создаем общий массив
            string[] result = new string[list[0].Length + list[1].Length + list[2].Length + list[3].Length + list[4].Length + list[5].Length];
            //группируем все в один массив
            for (int i = 0, k = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list[i].Length; j++)
                {
                    result[k] = list[i][j];
                    k++;
                }
            }
            return result;
        }

        private void PlayListBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            ((BlurEffect)GridMain.Effect).Radius = 15;
            GridWait.Visibility = System.Windows.Visibility.Visible;

            //получаем список путей файлов и папок дропнутых в программу
            string[] FileDropList = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            //создаем список для дальнейшей группировки
            List<string[]> list = new List<string[]>();
            int N = 0;
            //остсеиваем папки от файлов
            for (int i = 0; i < FileDropList.Length; i++)
            {
                if ((File.GetAttributes(FileDropList[i]) & FileAttributes.Directory) == FileAttributes.Directory) //если папка
                {
                    list.Add(GetFilesFromDirectory(FileDropList[i])); //получаем список файлов в этой папке и подпапках
                    N += list[i].Length;
                }
                else //иначе файл
                {
                    list.Add(new string[] { FileDropList[i] });
                    N++;
                }
            }
            //создаем общий массив
            string[] FileNames = new string[N];
            //группируем все в один массив
            for (int i = 0, k = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list[i].Length; j++)
                {
                    FileNames[k] = list[i][j];
                    k++;
                }
            }

            //добавляем треки или плейлист в текущий плейлист
            player.PlaylistUpdate(FileNames, MODE.ADD);
            LoadPlayList();//обновляем плейлист интерфейса
            player.SavePlayListToFile_m3u8("default.m3u8");//сохраняем плейлист по умолчанию

            GridWait.Visibility = System.Windows.Visibility.Hidden;
            ((BlurEffect)GridMain.Effect).Radius = 0;
        }

        private void MainMenuItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Grid MMI = sender as Grid;
            switch (Convert.ToInt32(MMI.Name.Substring(3)))
            {
                default:
                    break;
                case 0: //О программе
                    About AboutWindow = new About(); //Создание нового окна.
                    AboutWindow.Owner = this; // Назначение текущего окна владельцем.
                    AboutWindow.Show(); // Отображение окна, принадлежащего окну-владельцу.
                    break;
                case 1: //Справка
                    Help.ShowHelp(null, "AudioPlayer_Help_RU_v.1.1.chm", HelpNavigator.TableOfContents);
                    break;
                case 2: //Выход
                    this.Close();
                    break;
            }
        }

        private void MMI_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Grid MMI = sender as Grid;
            MMI.Background = new SolidColorBrush(Color.FromArgb(50, 50, 50, 50));
        }

        private void MMI_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Grid MMI = sender as Grid;
            MMI.Background = Brushes.Transparent;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            source = PresentationSource.FromVisual(this);
            dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
            PixelSize = 96.0 / dpiX;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.grid.Margin = new Thickness(7);
            else
                this.grid.Margin = new Thickness(0);
            this.ShowInTaskbar = (this.WindowState == WindowState.Minimized) ? false : true;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Spectr._SpectrParams.SWidth = (int)(GridVisualisation.ActualWidth * 0.008f);
            Spectr._SpectrParams.SDistance = (int)(GridVisualisation.ActualWidth * 0.002f);
            Spectr._SpectrParams.LinesCount = (int)((GridVisualisation.ActualWidth / (Spectr._SpectrParams.SWidth + Spectr._SpectrParams.SDistance)));
        }

        private void TopGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) //перемещение окна программы
        {
            this.DragMove();
        }

        private void MinButtonClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaxButtonClick(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button).Name == "MaxButton") 
            {
                MaxButton.Visibility = Visibility.Hidden;
                NormButton.Visibility = Visibility.Visible;
            }
            else
            {
                NormButton.Visibility = Visibility.Hidden;
                MaxButton.Visibility = Visibility.Visible;
            }
            this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Image_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MainMenu.Visibility = System.Windows.Visibility.Visible;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //MainMenu.Visibility = System.Windows.Visibility.Hidden;
        }

        ///////////////////////////////////////////////////////
        #region SizeChanging
        void OnSizeSouth(object sender, MouseButtonEventArgs e) { OnSize(sender, SizingAction.South); }
        void OnSizeNorth(object sender, MouseButtonEventArgs e) { OnSize(sender, SizingAction.North); }
        void OnSizeEast(object sender, MouseButtonEventArgs e) { OnSize(sender, SizingAction.East); }
        void OnSizeWest(object sender, MouseButtonEventArgs e) { OnSize(sender, SizingAction.West); }
        void OnSizeNorthWest(object sender, MouseButtonEventArgs e) { OnSize(sender, SizingAction.NorthWest); }
        void OnSizeNorthEast(object sender, MouseButtonEventArgs e) { OnSize(sender, SizingAction.NorthEast); }
        void OnSizeSouthEast(object sender, MouseButtonEventArgs e) { OnSize(sender, SizingAction.SouthEast); }
        void OnSizeSouthWest(object sender, MouseButtonEventArgs e) { OnSize(sender, SizingAction.SouthWest); }

        void OnSize(object sender, SizingAction action)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                if (this.WindowState == WindowState.Normal)
                    DragSize(new WindowInteropHelper(this).Handle, action);
        }

        const int WM_SYSCOMMAND = 0x112;
        const int SC_SIZE = 0xF000;
        const int SC_KEYMENU = 0xF100;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        void DragSize(IntPtr handle, SizingAction sizingAction)
        {
            SendMessage(handle, WM_SYSCOMMAND, (IntPtr)(SC_SIZE + sizingAction), IntPtr.Zero);
            SendMessage(handle, 514, IntPtr.Zero, IntPtr.Zero);
        }

        public enum SizingAction
        {
            North = 3,
            South = 6,
            East = 2,
            West = 1,
            NorthEast = 5,
            NorthWest = 4,
            SouthEast = 8,
            SouthWest = 7
        }
        #endregion

        
    }
}
