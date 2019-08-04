using System.Windows.Media;

namespace YL_Player
{
    public struct RGB
    {
        int r, g, b;
        
        public int R { set { r = value; } get { return r; } }
        public int G { set { g = value; } get { return g; } }
        public int B { set { b = value; } get { return b; } }

        public RGB(int r = 0, int g = 0, int b = 0)
            : this()
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static bool operator ==(RGB color1, RGB color2)
        {
            return (color1.R == color2.R && color1.G == color2.G && color1.B == color2.B);
        }
        public static bool operator !=(RGB color1, RGB color2)
        {
            return !(color1 == color2);
        }
        public static RGB operator +(RGB color1, RGB color2)
        {
            return new RGB(color1.R + color2.R, color1.G + color2.G, color1.B + color2.B);
        }
        public static RGB operator -(RGB color1, RGB color2)
        {
            return new RGB(color1.R - color2.R, color1.G - color2.G, color1.B - color2.B);
        }
    }

    public static class RGBExtensions
    {
        public static Color ToColor(this RGB color, int a = 255)
        {
            return Color.FromArgb((byte)a, (byte)color.R, (byte)color.G, (byte)color.B);
        }
    }
}
