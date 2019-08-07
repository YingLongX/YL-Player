using System.Windows.Media;

namespace YL_Player
{
    public struct RGB
    {
        public int R { set; get; }
        public int G { set; get; }
        public int B { set; get; }

        public RGB(int r = 0, int g = 0, int b = 0)
            : this()
        {
            this.R = r;
            this.G = g;
            this.B = b;
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

        public override bool Equals(object obj)
        {
            return obj is RGB rgb &&
                   R == rgb.R &&
                   G == rgb.G &&
                   B == rgb.B;
        }

        public override int GetHashCode()
        {
            var hashCode = -1520100960;
            hashCode = hashCode * -1521134295 + R.GetHashCode();
            hashCode = hashCode * -1521134295 + G.GetHashCode();
            hashCode = hashCode * -1521134295 + B.GetHashCode();
            return hashCode;
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
