using System;
using System.ComponentModel;
using System.Globalization;

namespace MediaBrowser.Plugins.SoundCloud.Drawing
{
    public struct SizeF
    {
        public readonly static SizeF Empty;

        private float width;

        private float height;

        public float Height
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
                return (this.width != 0f ? false : this.height == 0f);
            }
        }

        public float Width
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

        static SizeF()
        {
            SizeF.Empty = new SizeF();
        }

        public SizeF(SizeF size)
        {
            this.width = size.width;
            this.height = size.height;
        }

        public SizeF(PointF pt)
        {
            this.width = pt.X;
            this.height = pt.Y;
        }

        public SizeF(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        public static SizeF Add(SizeF sz1, SizeF sz2)
        {
            SizeF sizeF = new SizeF(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
            return sizeF;
        }

        public override bool Equals(object obj)
        {
            bool flag;
            if (obj is SizeF)
            {
                SizeF sizeF = (SizeF)obj;
                flag = (sizeF.Width != this.Width || sizeF.Height != this.Height ? false : sizeF.GetType().Equals(this.GetType()));
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public override int GetHashCode()
        {
            return this.GetHashCode();
        }

        public static SizeF operator +(SizeF sz1, SizeF sz2)
        {
            return SizeF.Add(sz1, sz2);
        }

        public static bool operator ==(SizeF sz1, SizeF sz2)
        {
            return (sz1.Width != sz2.Width ? false : sz1.Height == sz2.Height);
        }

        public static explicit operator PointF(SizeF size)
        {
            return new PointF(size.Width, size.Height);
        }

        public static bool operator !=(SizeF sz1, SizeF sz2)
        {
            return !(sz1 == sz2);
        }

        public static SizeF operator -(SizeF sz1, SizeF sz2)
        {
            return SizeF.Subtract(sz1, sz2);
        }

        public static SizeF Subtract(SizeF sz1, SizeF sz2)
        {
            SizeF sizeF = new SizeF(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
            return sizeF;
        }

        public PointF ToPointF()
        {
            return (PointF)this;
        }

        public Size ToSize()
        {
            return Size.Truncate(this);
        }

        public override string ToString()
        {
            return string.Concat(new string[] { "{Width=", this.width.ToString(CultureInfo.CurrentCulture), ", Height=", this.height.ToString(CultureInfo.CurrentCulture), "}" });
        }
    }
}
