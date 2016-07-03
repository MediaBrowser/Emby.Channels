using System;
using System.ComponentModel;
using System.Globalization;

namespace MediaBrowser.Plugins.SoundCloud.Drawing
{
    public struct Size
    {
        public readonly static Size Empty;

        private int width;

        private int height;

        public int Height
        {
            get
            {
                return this.height;
            }
            set
            {
                this.height = value;
            }
        }

        [Browsable(false)]
        public bool IsEmpty
        {
            get
            {
                return (this.width != 0 ? false : this.height == 0);
            }
        }

        public int Width
        {
            get
            {
                return this.width;
            }
            set
            {
                this.width = value;
            }
        }

        static Size()
        {
            Size.Empty = new Size();
        }

        public Size(Point pt)
        {
            this.width = pt.X;
            this.height = pt.Y;
        }

        public Size(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public static Size Add(Size sz1, Size sz2)
        {
            Size size = new Size(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
            return size;
        }

        public static Size Ceiling(SizeF value)
        {
            Size size = new Size((int)Math.Ceiling((double)value.Width), (int)Math.Ceiling((double)value.Height));
            return size;
        }

        public override bool Equals(object obj)
        {
            bool flag;
            if (obj is Size)
            {
                Size size = (Size)obj;
                flag = (size.width != this.width ? false : size.height == this.height);
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public override int GetHashCode()
        {
            return this.width ^ this.height;
        }

        public static Size operator +(Size sz1, Size sz2)
        {
            return Size.Add(sz1, sz2);
        }

        public static bool operator ==(Size sz1, Size sz2)
        {
            return (sz1.Width != sz2.Width ? false : sz1.Height == sz2.Height);
        }

        public static explicit operator Point(Size size)
        {
            return new Point(size.Width, size.Height);
        }

        public static implicit operator SizeF(Size p)
        {
            return new SizeF((float)p.Width, (float)p.Height);
        }

        public static bool operator !=(Size sz1, Size sz2)
        {
            return !(sz1 == sz2);
        }

        public static Size operator -(Size sz1, Size sz2)
        {
            return Size.Subtract(sz1, sz2);
        }

        public static Size Round(SizeF value)
        {
            Size size = new Size((int)Math.Round((double)value.Width), (int)Math.Round((double)value.Height));
            return size;
        }

        public static Size Subtract(Size sz1, Size sz2)
        {
            Size size = new Size(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
            return size;
        }

        public override string ToString()
        {
            return string.Concat(new string[] { "{Width=", this.width.ToString(CultureInfo.CurrentCulture), ", Height=", this.height.ToString(CultureInfo.CurrentCulture), "}" });
        }

        public static Size Truncate(SizeF value)
        {
            return new Size((int)value.Width, (int)value.Height);
        }
    }
}
