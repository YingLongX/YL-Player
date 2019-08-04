using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YL_Player
{
    public enum VISUALISATION_MODE
    {
        COLOR,
        RAINBOW
    };

    public class SpectrParams
    {
        public int FPS; //fps //16ms = 60fps|25ms = 40fps|33ms = 30fps
        public int LinesCount;
        public int SWidth;
        public float SHeight;
        public int SDistance;
        public int RainbowFX; //fx = 1, 3, 5, 15
        public double Opacity;
        public Color SColor;

        public SpectrParams(int FPS = 40, int LinesCount = 128, int SWidth = 3, float SHeight = 0.2f, int SDistance = 1, byte RainbowFX = 15, double Opacity = 1.0, Color? SColor = null)
        {
            this.FPS = FPS; //fps //16ms = 60fps|25ms = 40fps|33ms = 30fps
            this.LinesCount = LinesCount;
            this.SWidth = SWidth;
            this.SHeight = SHeight;
            this.SDistance = SDistance;
            this.RainbowFX = RainbowFX; //fx = 1, 3, 5, 15
            this.Opacity = Opacity;
            this.SColor = SColor ?? Color.FromArgb(255, 255, 255, 255);
        }
    }

    public class Visualization
    {
        private int[] Ydata = new int[1024]; //буфер FFTdata
        public SpectrParams _SpectrParams; //параметры
        public Canvas canvas; //холст
        
        //конструктор с параметрами по умолчанию
        public Visualization(Canvas canvas)
        {
            this.canvas = canvas;
            _SpectrParams = new SpectrParams();
        }
        //конструктор с параметрами
        public Visualization(Canvas canvas, SpectrParams sp)
        {
            this.canvas = canvas;
            _SpectrParams = sp;
        }

        private void DrawRect(int i, int w, int h, Color color)
        {
            Rectangle myRect = new Rectangle();

            Canvas.SetLeft(myRect, i);
            Canvas.SetBottom(myRect, 0);
            myRect.Height = h;
            myRect.Width = w;
            myRect.Fill = new SolidColorBrush(color);
            myRect.Opacity = _SpectrParams.Opacity;
            canvas.Children.Add(myRect);
            /*{
                DropShadowEffect dse = new DropShadowEffect();
                dse.ShadowDepth = 0;
                dse.Direction = 0;
                dse.Color = SColor;
                dse.BlurRadius = 50;
                dse.Opacity = 0.75d;
                _SpectrGrid.Effect = dse;
            }*/
        }

        /// <summary>
        /// Функция отрисовки спектра
        /// </summary>
        /// <param name="FFTdata">Быстрое преобразование Фурье</param>
        /// <param name="mode">Режим визуализации: (цвет/радуга) по умолчанию радуга</param>
        public void Spectrum(float[] FFTdata, VISUALISATION_MODE mode = VISUALISATION_MODE.RAINBOW)
        {
            canvas.Children.Clear();
            
            RGB color = new RGB(255, 0, 0);

            int bar = 0, y = 0, b0 = 0;
            float peak;
            double k = 10.0 / (_SpectrParams.LinesCount - 1);
            for (int x = 0; x < _SpectrParams.LinesCount; ++x)
            {
                peak = 0;
                int b1 = (int)Math.Pow(2, x * k);
                if (b1 > 1023) b1 = 1023;
                else if (b1 <= b0) b1 = b0 + 1;
                for (; b0 < b1; ++b0)
                    if (peak < FFTdata[1 + b0])
                        peak = FFTdata[1 + b0];
                y = (int)(Math.Sqrt(peak) * 765 - 4);//765 = 3 * 255
                if (y > 255) y = 255;
                else if (y < 0) y = 0;

                if (y >= Ydata[x]) Ydata[x] = y;
                else
                {
                    if (Ydata[x] > 0) Ydata[x] -= 10;
                    /*int low = (int)(40 / (256 - Ydata[x]));
                    if (Ydata[x] > 0) Ydata[x] -= (low > 0) ? low : 1;*/
                    y = Ydata[x];
                    if (y < 0) y = 0;
                }

                switch (mode)
                {
                    default: break;
                    case VISUALISATION_MODE.COLOR: DrawRect(bar, _SpectrParams.SWidth, (int)(y * _SpectrParams.SHeight), _SpectrParams.SColor);
                        break;
                    case VISUALISATION_MODE.RAINBOW:
                        if      (color.R == 255 && color.G <  255 && color.B == 0  ) color += new RGB(0, _SpectrParams.RainbowFX, 0);
                        else if (color.R >  0   && color.G == 255 && color.B == 0  ) color += new RGB(-_SpectrParams.RainbowFX, 0, 0);
                        else if (color.R == 0   && color.G == 255 && color.B <  255) color += new RGB(0, 0, _SpectrParams.RainbowFX);
                        else if (color.R == 0   && color.G >  0   && color.B == 255) color += new RGB(0, -_SpectrParams.RainbowFX, 0);
                        else if (color.R <  255 && color.G == 0   && color.B == 255) color += new RGB(_SpectrParams.RainbowFX, 0, 0);
                        else if (color.R == 255 && color.G == 0   && color.B >  0  ) color += new RGB(0, 0, -_SpectrParams.RainbowFX);

                        DrawRect(bar, _SpectrParams.SWidth, (int)(y * _SpectrParams.SHeight), color.ToColor());
                        break;
                }
                bar += _SpectrParams.SWidth + _SpectrParams.SDistance;
            }
        }
    }
}
