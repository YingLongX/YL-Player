﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Tags;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;

namespace YL_Player
{
    public delegate void OnNextSongDelegate();
    public delegate void OnShowDurationDelegate(int duration); //делегат функции
    public delegate void OnFileNotFoundDelegate(int index); //делегат функции события файл не найден
    public enum MODE //режимы
    {
        ADD,
        UPDATE
    }
    public class Audio //класс элементов плейлиста
    {
        public int duration { get; set; } //длительность в секундах
        public string artist { get; set; } //исполнитель
        public string title { get; set; } //название
        public string path { get; set; } //путь к файлу (или url если это поток)
        public Image image { get; set; } //AlbumArt
    }

    public class Player //класс плеера
    {
        public int stream; //поток

        public List<Audio> PlayList = new List<Audio>(); //плейлист

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer(); //таймер

        private int[] fxBands = new int[18];
        private float[] gains = new float[18];
        private float balance = 0f;
        //private int fxReverb;

        //оъявляем делегаты
        public OnNextSongDelegate NextSong;
        public OnShowDurationDelegate ShowDuration;
        public OnFileNotFoundDelegate OnFileNotFound;
        public float volume = 1.0f;//громкость
        public bool pause = false;//флаг паузы
        public int CurrentPlayingIndex = -1;//текущий трек
        Dictionary<string, System.Drawing.Bitmap> RecordImages;

        public Player(OnNextSongDelegate NextSongFunc,
                      OnShowDurationDelegate ShowDurationFunc,
                      OnFileNotFoundDelegate OnFileNotFoundFunc) //конструктор
        {
            //BassNet Registration
			// You need to use your own <license_email_address> and <license_code>
			// You can register your BASS.NET license here: http://bass.radio42.com/bass_register.html
            BassNet.Registration("<license_email_address>", "<license_code>"); //This hides splash screen BassNet on startup.
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
			{
				throw new Exception("BASS failed to initialize");
			}

            timer.Interval = 1000;
            timer.Tick += new EventHandler(Timer_Tick);

            for (int i = 0; i < 18; i++)
                gains[i] = 0f; //задаем значения эквалайзера поумолчанию

            NextSong = NextSongFunc;
            ShowDuration = ShowDurationFunc;
            OnFileNotFound = OnFileNotFoundFunc;

            RecordImages = new Dictionary<string, System.Drawing.Bitmap>();
            RecordImages.Add("http://air.radiorecord.ru:805/rr_320", Properties.Resources._0);
            RecordImages.Add("http://air.radiorecord.ru:805/trancehits_320", Properties.Resources._1);
            RecordImages.Add("http://air.radiorecord.ru:805/2step_320", Properties.Resources._2);
            RecordImages.Add("http://air.radiorecord.ru:805/tecktonik_320", Properties.Resources._3);
            RecordImages.Add("http://air.radiorecord.ru:805/neurofunk_320", Properties.Resources._4);
            RecordImages.Add("http://air.radiorecord.ru:805/hypno_320", Properties.Resources._5);
            RecordImages.Add("http://air.radiorecord.ru:805/trancehouse_320", Properties.Resources._6);
            RecordImages.Add("http://air.radiorecord.ru:805/edmhits_320", Properties.Resources._7);
            RecordImages.Add("http://air.radiorecord.ru:805/houseclss_320", Properties.Resources._8);
            RecordImages.Add("http://air.radiorecord.ru:805/uplift_320", Properties.Resources._9);
            RecordImages.Add("http://air.radiorecord.ru:805/darkside_320", Properties.Resources._10);
            RecordImages.Add("http://air.radiorecord.ru:805/dream_320", Properties.Resources._11);
            RecordImages.Add("http://air.radiorecord.ru:805/bighits_320", Properties.Resources._12);
            RecordImages.Add("http://air.radiorecord.ru:805/househits_320", Properties.Resources._13);
            RecordImages.Add("http://air.radiorecord.ru:805/synth_320", Properties.Resources._14);
            RecordImages.Add("http://air.radiorecord.ru:805/progr_320", Properties.Resources._15);
            RecordImages.Add("http://air.radiorecord.ru:805/jackin_320", Properties.Resources._16);
            RecordImages.Add("http://air.radiorecord.ru:805/mmbt_320", Properties.Resources._17);
            RecordImages.Add("http://air.radiorecord.ru:805/mt_320", Properties.Resources._18);
            RecordImages.Add("http://air.radiorecord.ru:805/elect_320", Properties.Resources._19);
            RecordImages.Add("http://air.radiorecord.ru:805/mf_320", Properties.Resources._20);
            RecordImages.Add("http://air.radiorecord.ru:805/ibiza_320", Properties.Resources._21);
            RecordImages.Add("http://air.radiorecord.ru:805/gold_320", Properties.Resources._22);
            RecordImages.Add("http://air.radiorecord.ru:805/chillhouse_320", Properties.Resources._23);
            RecordImages.Add("http://air.radiorecord.ru:805/1980_320", Properties.Resources._24);
            RecordImages.Add("http://air.radiorecord.ru:805/cadillac_320", Properties.Resources._25);
            RecordImages.Add("http://air.radiorecord.ru:805/rapclassics_320", Properties.Resources._26);
            RecordImages.Add("http://air.radiorecord.ru:805/rap_320", Properties.Resources._27);
            RecordImages.Add("http://air.radiorecord.ru:805/discofunk_320", Properties.Resources._28);
            RecordImages.Add("http://air.radiorecord.ru:805/technopop_320", Properties.Resources._29);
            RecordImages.Add("http://air.radiorecord.ru:805/eurodance_320", Properties.Resources._30);
            RecordImages.Add("http://air.radiorecord.ru:805/russiangold_320", Properties.Resources._31);
            RecordImages.Add("http://air.radiorecord.ru:805/drumhits_320", Properties.Resources._32);
            RecordImages.Add("http://air.radiorecord.ru:805/liquidfunk_320", Properties.Resources._33);
            RecordImages.Add("http://air.radiorecord.ru:805/jungle_320", Properties.Resources._34);
            RecordImages.Add("http://air.radiorecord.ru:805/mix_320", Properties.Resources._35);
            RecordImages.Add("http://air.radiorecord.ru:805/deep_320", Properties.Resources._36);
            RecordImages.Add("http://air.radiorecord.ru:805/club_320", Properties.Resources._37);
            RecordImages.Add("http://air.radiorecord.ru:805/trop_320", Properties.Resources._38);
            RecordImages.Add("http://air.radiorecord.ru:805/goa_320", Properties.Resources._39);
            RecordImages.Add("http://air.radiorecord.ru:805/fut_320", Properties.Resources._40);
            RecordImages.Add("http://air.radiorecord.ru:805/tm_320", Properties.Resources._41);
            RecordImages.Add("http://air.radiorecord.ru:805/chil_320", Properties.Resources._42);
            RecordImages.Add("http://air.radiorecord.ru:805/mini_320", Properties.Resources._43);
            RecordImages.Add("http://air.radiorecord.ru:805/ps_320", Properties.Resources._44);
            RecordImages.Add("http://air.radiorecord.ru:805/rus_320", Properties.Resources._45);
            RecordImages.Add("http://air.radiorecord.ru:805/vip_320", Properties.Resources._46);
            RecordImages.Add("http://air.radiorecord.ru:805/sd90_320", Properties.Resources._47);
            RecordImages.Add("http://air.radiorecord.ru:805/brks_320", Properties.Resources._48);
            RecordImages.Add("http://air.radiorecord.ru:805/dub_320", Properties.Resources._49);
            RecordImages.Add("http://air.radiorecord.ru:805/dc_320", Properties.Resources._50);
            RecordImages.Add("http://air.radiorecord.ru:805/fbass_320", Properties.Resources._51);
            RecordImages.Add("http://air.radiorecord.ru:805/rmx_320", Properties.Resources._52);
            RecordImages.Add("http://air.radiorecord.ru:805/techno_320", Properties.Resources._53);
            RecordImages.Add("http://air.radiorecord.ru:805/hbass_320", Properties.Resources._54);
            RecordImages.Add("http://air.radiorecord.ru:805/teo_320", Properties.Resources._55);
            RecordImages.Add("http://air.radiorecord.ru:805/trap_320", Properties.Resources._56);
            RecordImages.Add("http://air.radiorecord.ru:805/pump_320", Properties.Resources._57);
            RecordImages.Add("http://air.radiorecord.ru:805/rock_320", Properties.Resources._58);
            RecordImages.Add("http://air.radiorecord.ru:805/mdl_320", Properties.Resources._59);
            RecordImages.Add("http://air.radiorecord.ru:805/symph_320", Properties.Resources._60);
            RecordImages.Add("http://air.radiorecord.ru:805/gop_320", Properties.Resources._61);
            RecordImages.Add("http://air.radiorecord.ru:805/yo_320", Properties.Resources._62);
            RecordImages.Add("http://air.radiorecord.ru:805/rave_320", Properties.Resources._63);
            RecordImages.Add("http://air.radiorecord.ru:805/gast_320", Properties.Resources._64);
            RecordImages.Add("http://air.radiorecord.ru:805/ansh_320", Properties.Resources._65);
            RecordImages.Add("http://air.radiorecord.ru:805/naft_320", Properties.Resources._66);
        }
        ~Player() //деструктор
        {
            Bass.BASS_StreamFree(stream);//освобождаем поток
            Bass.BASS_Free();//освобождаем ресурсы
        }

        /// <summary>
        /// воспроизвести из плейлиста трек с индексом index
        /// </summary>
        /// <param name="index"></param>
        public void Play(int index)
        {
            timer.Stop();
            pause = false;
            if (PlayList.Count == 0) return; //если плейлист пуст, то нечего воспроизводить
            if (!File.Exists(PlayList[index].path) && !PlayList[index].path.StartsWith("http")) //если файл не найден и это не ссылка на поток
            {
                OnFileNotFound(index); //вызываем событие
                return;
            }

            Bass.BASS_ChannelPause(stream);//приостанавливаем воспроизведение
            Bass.BASS_StreamFree(stream);//освобождаем поток

            if (PlayList[index].path.StartsWith("http")) //если ссылка на поток
            {
                //создаем поток
                stream = Bass.BASS_StreamCreateURL(PlayList[index].path, 0, BASSFlag.BASS_STREAM_STATUS, null, IntPtr.Zero);
                if (stream != 0)//если успешно
                {
                    SetEQ(gains);//задаем настройки эквалайзера для потока
                    SetVolume(volume);//задаем громкость
                    SetBalance(balance);//задаем баланс
                    Bass.BASS_ChannelPlay(stream, true);//воспроизводим
                }
            }
            else //иначе файл
            {
                //создаем поток
                stream = Bass.BASS_StreamCreateFile(PlayList[index].path, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN);
                if (stream != 0)//если успешно
                {
                    SetEQ(gains);
                    SetVolume(volume);//задаем громкость
                    SetBalance(balance);//задаем баланс
                    Bass.BASS_ChannelPlay(stream, false);//воспроизводим
                }
            }
            CurrentPlayingIndex = index;//текущий индекс
            timer.Start();//запускаем таймер
        }
        /// <summary>
        /// Получить время воспроизведения
        /// </summary>
        /// <returns></returns>
        public int GetDurationOfStream()
        {
            long time = Bass.BASS_ChannelGetLength(stream);
            return (int)Bass.BASS_ChannelBytes2Seconds(stream, time);
        }
        /// <summary>
        /// Получить текущую позицию воспроизведения
        /// </summary>
        /// <returns></returns>
        public int GetCurrentPlayingPos()
        {
            long pos = Bass.BASS_ChannelGetPosition(stream);
            return (int)Bass.BASS_ChannelBytes2Seconds(stream, pos);
        }
        /// <summary>
        /// Задать позицию воспроизведения
        /// </summary>
        /// <param name="pos"></param>
        public void SetPosOfScroll(int pos)
        {
            Bass.BASS_ChannelSetPosition(stream, (double)pos);
        }
        /// <summary>
        /// задать громкость
        /// </summary>
        /// <param name="vol"></param>
        public void SetVolume(float vol)
        {
            volume = vol;
            //if (stream != 0)//если поток не пуст, задаем громкость
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, volume);
        }
        /// <summary>
        /// Тик таймера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            //если CurrentPlayingIndex вне границ массива, то останавливаем таймер
            if (CurrentPlayingIndex >= PlayList.Count || CurrentPlayingIndex < 0) { timer.Stop(); return; }
            //если воспроизводится потоковое аудио, то продолжаем воспроизводить
            if (PlayList[CurrentPlayingIndex].duration == -1) { ShowDuration(GetCurrentPlayingPos()); return; }
            //если трек не кончился, то показываем время воспроизведения
            if (GetCurrentPlayingPos() < GetDurationOfStream())
                ShowDuration(GetCurrentPlayingPos());
            else //иначе
            {
                timer.Stop();   //останавливаем таймер
                NextSong();     //и переключаем трек
            }
        }
        /// <summary>
        /// продолжить воспроизведение
        /// </summary>
        /// <param name="index"></param>
        public void Continue(int index)
        {
            pause = false;
            Bass.BASS_ChannelPlay(stream, false);
        }
        /// <summary>
        /// пауза
        /// </summary>
        public void Pause()
        {
            pause = true;
            Bass.BASS_ChannelPause(stream);//Pause        
        }
        /// <summary>
        /// стоп
        /// </summary>
        public void Stop()
        {
            Bass.BASS_ChannelStop(stream);
        }
        /// <summary>
        /// Воспроизвести/пауза
        /// </summary>
        /// <param name="index"></param>
        public void PlayPause(int index)
        {
            if (pause) Continue(index);
            else Pause();
        }
        /// <summary>
        /// Получить метаданные
        /// </summary>
        /// <returns></returns>
        public String DoMeta()
        {
            String RDS_1 = "";
            String RDS_2 = "";

            IntPtr meta = Bass.BASS_ChannelGetTags(stream, BASSTag.BASS_TAG_META);
            if (meta != IntPtr.Zero)
            {
                string str = Utils.IntPtrAsStringUtf8(meta);
                if (str.Length > 15)
                {
                    RDS_2 = str.Substring(13, str.Length - 15);
                    if (String.Compare(RDS_2, RDS_2.Length - 2, "='", 0, 2) == 0)
                    {
                        string str2 = RDS_2.Substring(0, RDS_2.Length - 13);
                        RDS_2 = str2;
                    }
                    if (RDS_2.Length > 0 && RDS_2.Length < 64)
                    {
                        return RDS_2;
                    }
                    else
                        return RDS_1;
                }
                else
                    return RDS_1;
            }
            else
                return RDS_1;
        }
        /// <summary>
        /// Обновить плейлист
        /// </summary>
        /// <param name="files"></param>
        /// <param name="mode"></param>
        public void PlaylistUpdate(String[] files, MODE mode)
        {
            if (mode == MODE.UPDATE) PlayList.Clear();

            for (int i = 0; i < files.Length; i++)
            {
                if (File.Exists(files[i]))//если файл существует
                {   //если плейлист, то загружаем
                    if (files[i].EndsWith(".m3u") || files[i].EndsWith(".m3u8")) GetPlayListFromFile_m3u(files[i]);
                    else//иначе это файл
                    {
                        TagLib.File tagFile = TagLib.File.Create(files[i]); //читаем тег из файла
                        PlayList.Add(new Audio()); //выделяем память под новый элемент
                        PlayList[PlayList.Count - 1].path = files[i];//путь к файлу
                        PlayList[PlayList.Count - 1].artist = tagFile.Tag.FirstAlbumArtist;//артист
                        PlayList[PlayList.Count - 1].title = tagFile.Tag.Title;//название
                        PlayList[PlayList.Count - 1].duration = (int)tagFile.Properties.Duration.TotalSeconds;
                        PlayList[PlayList.Count - 1].image = GetAlbumArt(files[i]);
                    }
                }
            }
        }
        /// <summary>
        /// Считать альбомарт из файла
        /// если нет данных вернет null
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private Image GetAlbumArt(string path)
        {
            Image img = null;
            if (!path.StartsWith("http")) //если не поток
            {
                int _stream = Bass.BASS_StreamCreateFile(path, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN);
                if (_stream != 0)//если успешно
                {
                    TAG_INFO tagInfo = new TAG_INFO(path);
                    if (BassTags.BASS_TAG_GetFromFile(_stream, tagInfo))
                        img = (Image)tagInfo.PictureGetImage(0);
                }
                Bass.BASS_StreamFree(_stream);//освобождаем поток
            }
            else
            {
                if (RecordImages.ContainsKey(path))
                {
                    img = RecordImages[path];
                }
            }
            return img;
        }

        /// <summary>
        /// Изменить кодировку строки
        /// </summary>
        /// <param name="str"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private String EncodeString(String str, Encoding from, Encoding to)
        {
            return to.GetString(Encoding.Convert(from, to, from.GetBytes(str)));
        }
        /// <summary>
        /// загрузить плейлист из файла m3u или m3u8
        /// </summary>
        /// <param name="playlistFileName"></param>
        public void GetPlayListFromFile_m3u(String playlistFileName)
        {
            using (StreamReader sr = File.OpenText(playlistFileName))
            {
                Encoding encoding; //в соответствии в форматом выбираем кодировку
                if (playlistFileName.EndsWith(".m3u"))
                    encoding = Encoding.GetEncoding(1252);
                else encoding = Encoding.UTF8;

                String strPlList = EncodeString(sr.ReadToEnd(), encoding, Encoding.Default);
                String[] lines = strPlList.Split(new String[] { "\n", "\r", "\n\r", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (lines[0].Trim().ToUpper() == "#EXTM3U")
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("#EXTINF", StringComparison.CurrentCultureIgnoreCase))
                        {
                            String[] info = lines[i].Split(new String[] { ":", "," }, StringSplitOptions.None);
                            if (lines[i + 1].StartsWith("http") || File.Exists(lines[i + 1]))//если файл существует, то добавляем в плейлист
                            {
                                PlayList.Add(new Audio());
                                PlayList[PlayList.Count - 1].path = lines[i + 1];//путь к файлу
                                switch (info.Length)
                                {
                                    case 2:
                                        PlayList[PlayList.Count - 1].title = info[1];//название
                                        TagLib.File tagFile = TagLib.File.Create(PlayList[PlayList.Count - 1].path); //читаем тег из файла
                                        PlayList[PlayList.Count - 1].duration = (int)tagFile.Properties.Duration.TotalSeconds;
                                        break;
                                    case 3:
                                        PlayList[PlayList.Count - 1].duration = Convert.ToInt32(info[1]);
                                        PlayList[PlayList.Count - 1].title = info[2];//название
                                        break;
                                    case 4:

                                        PlayList[PlayList.Count - 1].duration = Convert.ToInt32(info[1]);
                                        PlayList[PlayList.Count - 1].artist = info[2];
                                        PlayList[PlayList.Count - 1].title = info[3];//название
                                        break;
                                }
                                PlayList[PlayList.Count - 1].image = GetAlbumArt(PlayList[PlayList.Count - 1].path);
                            }
                        }
                        else if (lines[i].StartsWith("# ", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (lines[i + 1].StartsWith("http") || File.Exists(lines[i + 1]))//если файл существует, то добавляем в плейлист
                            {
                                PlayList.Add(new Audio());
                                PlayList[PlayList.Count - 1].path = lines[i + 1];//путь к файлу
                                PlayList[PlayList.Count - 1].title = lines[i].Substring(2);
                                TagLib.File tagFile = TagLib.File.Create(PlayList[PlayList.Count - 1].path); //читаем тег из файла
                                PlayList[PlayList.Count - 1].duration = (int)tagFile.Properties.Duration.TotalSeconds;
                                PlayList[PlayList.Count - 1].image = GetAlbumArt(PlayList[PlayList.Count - 1].path);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Trim().StartsWith("http") || File.Exists(lines[i].Trim()))//если файл существует, то добавляем в плейлист
                        {
                            PlayList.Add(new Audio());
                            PlayList[PlayList.Count - 1].path = lines[i].Trim();//путь к файлу
                            PlayList[PlayList.Count - 1].image = GetAlbumArt(PlayList[PlayList.Count - 1].path);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// сохранить плейлист
        /// </summary>
        /// <param name="playlistFileName"></param>
        public void SavePlayListToFile_m3u8(String playlistFileName)
        {
            using (StreamWriter file = new StreamWriter(playlistFileName))
            {
                file.WriteLine("#EXTM3U");
                for (int i = 0; i < PlayList.Count; i++)
                {
                    String line = "#EXTINF:" + PlayList[i].duration.ToString() + "," +
                        ((PlayList[i].artist != null) ? PlayList[i].artist + "," : "") +
                        ((PlayList[i].title != null) ? PlayList[i].title : PlayList[i].path.Substring(PlayList[i].path.LastIndexOf('\\') + 1, PlayList[i].path.Length - PlayList[i].path.LastIndexOf('\\') - 5));
                    file.WriteLine(EncodeString(line, Encoding.Default, Encoding.UTF8));
                    file.WriteLine(EncodeString(PlayList[i].path, Encoding.Default, Encoding.UTF8));
                }
            }
        }

        public float[] GetFFTData()
        {
            float[] fftdata = new float[1024];
            if (!pause)
                Bass.BASS_ChannelGetData(stream, fftdata, (int)BASSData.BASS_DATA_FFT2048);
            else
            {
                for (int i = 0; i < 1024; i++)
                    fftdata[i] = 0;
            }
            return fftdata;
        }

        public void SetBalance(float _balance)
        {
            balance = _balance;
            //if (stream != 0)
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_PAN, _balance); //задаем баланс
        }

        public void SetEQ(float[] _gains)
        {
            //if (stream != 0)
            {
                for (int i = 0; i < 18; i++)
                {
                    fxBands[i] = Bass.BASS_ChannelSetFX(stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
                    gains[i] = _gains[i];
                }

                BASS_DX8_PARAMEQ p = new BASS_DX8_PARAMEQ();

                Bass.BASS_FXSetParameters(fxBands[0], new BASS_DX8_PARAMEQ(31f, 1f, gains[0]));
                Bass.BASS_FXSetParameters(fxBands[1], new BASS_DX8_PARAMEQ(63f, 2f, gains[1]));
                Bass.BASS_FXSetParameters(fxBands[2], new BASS_DX8_PARAMEQ(87f, 3f, gains[2]));
                Bass.BASS_FXSetParameters(fxBands[3], new BASS_DX8_PARAMEQ(125f, 4f, gains[3]));
                Bass.BASS_FXSetParameters(fxBands[4], new BASS_DX8_PARAMEQ(175f, 5f, gains[4]));
                Bass.BASS_FXSetParameters(fxBands[5], new BASS_DX8_PARAMEQ(250f, 6f, gains[5]));
                Bass.BASS_FXSetParameters(fxBands[6], new BASS_DX8_PARAMEQ(350f, 7f, gains[6]));
                Bass.BASS_FXSetParameters(fxBands[7], new BASS_DX8_PARAMEQ(500f, 8f, gains[7]));
                Bass.BASS_FXSetParameters(fxBands[8], new BASS_DX8_PARAMEQ(700f, 9f, gains[8]));
                Bass.BASS_FXSetParameters(fxBands[9], new BASS_DX8_PARAMEQ(1000f, 10f, gains[9]));
                Bass.BASS_FXSetParameters(fxBands[10], new BASS_DX8_PARAMEQ(1400f, 11f, gains[10]));
                Bass.BASS_FXSetParameters(fxBands[11], new BASS_DX8_PARAMEQ(2000f, 12f, gains[11]));
                Bass.BASS_FXSetParameters(fxBands[12], new BASS_DX8_PARAMEQ(2800f, 13f, gains[12]));
                Bass.BASS_FXSetParameters(fxBands[13], new BASS_DX8_PARAMEQ(4000f, 14f, gains[13]));
                Bass.BASS_FXSetParameters(fxBands[14], new BASS_DX8_PARAMEQ(5600f, 15f, gains[14]));
                Bass.BASS_FXSetParameters(fxBands[15], new BASS_DX8_PARAMEQ(8000f, 16f, gains[15]));
                Bass.BASS_FXSetParameters(fxBands[16], new BASS_DX8_PARAMEQ(11200f, 17f, gains[16]));
                Bass.BASS_FXSetParameters(fxBands[17], new BASS_DX8_PARAMEQ(16000f, 18f, gains[17]));
            }
        }

        //public void SetReverb(int progress)
        //{
        //    BASS_DX8_REVERB p = new BASS_DX8_REVERB();
        //    Bass.BASS_FXGetParameters(fxReverb, p);
        //    p.fReverbMix = (float)(progress > 15 ? Math.Log((double)progress / 20.0) * 20.0 : -96.0);
        //    Bass.BASS_FXSetParameters(fxReverb, p);
        //}

        public void UpdateEQ(int band, float gain)
        {
            BASS_DX8_PARAMEQ p = new BASS_DX8_PARAMEQ();
            if (Bass.BASS_FXGetParameters(fxBands[band], p))
            {
                gains[band] = gain;
                p.fGain = gain;
                Bass.BASS_FXSetParameters(fxBands[band], p);
            }
        }
    }
}