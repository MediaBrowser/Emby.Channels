using System;
using System.ComponentModel;
using System.Globalization;

namespace MediaBrowser.Plugins.SoundCloud.Drawing
{
    public struct PointF
    {
        public readonly static PointF Empty;

        private float x;

        private float y;

        [Browsable(false)]
        public bool IsEmpty
        {
            get
            {
                return (this.x != 0f ? false : this.y == 0f);
            }
        }

        public float X
        {
            get
            {
                return this.x;
            }
            set
            {
                this.x = value;
            }
        }

        public float Y
        {
            get
            {
                return this.y;
            }
            set
            {
                this.y = value;
            }
        }

        static PointF()
        {
            PointF.Empty = new PointF();
        }

        public PointF(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static PointF Add(PointF pt, Size sz)
        {
            PointF pointF = new PointF(pt.X + (float)sz.Width, pt.Y + (float)sz.Height);
            return pointF;
        }

        public static PointF Add(PointF pt, SizeF sz)
        {
            PointF pointF = new PointF(pt.X + sz.Width, pt.Y + sz.Height);
            return pointF;
        }

        public override bool Equals(object obj)
        {
            bool flag;
            if (obj is PointF)
            {
                PointF pointF = (PointF)obj;
                flag = (pointF.X != this.X || pointF.Y != this.Y ? false : pointF.GetType().Equals(this.GetType()));
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

        public static PointF operator +(PointF pt, Size sz)
        {
            return PointF.Add(pt, sz);
        }

        public static PointF operator +(PointF pt, SizeF sz)
        {
            return PointF.Add(pt, sz);
        }

        public static bool operator ==(PointF left, PointF right)
        {
            return (left.X != right.X ? false : left.Y == right.Y);
        }

        public static bool operator !=(PointF left, PointF right)
        {
            return !(left == right);
        }

        public static PointF operator -(PointF pt, Size sz)
        {
            return PointF.Subtract(pt, sz);
        }

        public static PointF operator -(PointF pt, SizeF sz)
        {
            return PointF.Subtract(pt, sz);
        }

        public static PointF Subtract(PointF pt, Size sz)
        {
            PointF pointF = new PointF(pt.X - (float)sz.Width, pt.Y - (float)sz.Height);
            return pointF;
        }

        public static PointF Subtract(PointF pt, SizeF sz)
        {
            PointF pointF = new PointF(pt.X - sz.Width, pt.Y - sz.Height);
            return pointF;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{X={0}, Y={1}}}", new object[] { this.x, this.y });
        }
    }
}
