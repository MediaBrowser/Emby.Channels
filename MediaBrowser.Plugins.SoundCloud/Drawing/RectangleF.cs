using System;
using System.ComponentModel;
using System.Globalization;

namespace MediaBrowser.Plugins.SoundCloud.Drawing
{
    public struct RectangleF
    {
        public readonly static RectangleF Empty;

        private float x;

        private float y;

        private float width;

        private float height;

        [Browsable(false)]
        public float Bottom
        {
            get
            {
                return this.Y + this.Height;
            }
        }

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
                return (this.Width <= 0f ? true : this.Height <= 0f);
            }
        }

        [Browsable(false)]
        public float Left
        {
            get
            {
                return this.X;
            }
        }

        [Browsable(false)]
        public PointF Location
        {
            get
            {
                return new PointF(this.X, this.Y);
            }
            set
            {
                this.X = value.X;
                this.Y = value.Y;
            }
        }

        [Browsable(false)]
        public float Right
        {
            get
            {
                return this.X + this.Width;
            }
        }

        [Browsable(false)]
        public SizeF Size
        {
            get
            {
                return new SizeF(this.Width, this.Height);
            }
            set
            {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }

        [Browsable(false)]
        public float Top
        {
            get
            {
                return this.Y;
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

        static RectangleF()
        {
            RectangleF.Empty = new RectangleF();
        }

        public RectangleF(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public RectangleF(PointF location, SizeF size)
        {
            this.x = location.X;
            this.y = location.Y;
            this.width = size.Width;
            this.height = size.Height;
        }

        public bool Contains(float x, float y)
        {
            return (this.X > x || x >= this.X + this.Width || this.Y > y ? false : y < this.Y + this.Height);
        }

        public bool Contains(PointF pt)
        {
            return this.Contains(pt.X, pt.Y);
        }

        public bool Contains(RectangleF rect)
        {
            return (this.X > rect.X || rect.X + rect.Width > this.X + this.Width || this.Y > rect.Y ? false : rect.Y + rect.Height <= this.Y + this.Height);
        }

        public override bool Equals(object obj)
        {
            bool flag;
            if (obj is RectangleF)
            {
                RectangleF rectangleF = (RectangleF)obj;
                flag = (rectangleF.X != this.X || rectangleF.Y != this.Y || rectangleF.Width != this.Width ? false : rectangleF.Height == this.Height);
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public static RectangleF FromLTRB(float left, float top, float right, float bottom)
        {
            RectangleF rectangleF = new RectangleF(left, top, right - left, bottom - top);
            return rectangleF;
        }

        public override int GetHashCode()
        {
            int x = (int)((uint)this.X ^ ((uint)this.Y << 13 | (uint)this.Y >> 19) ^ ((uint)this.Width << 26 | (uint)this.Width >> 6) ^ ((uint)this.Height << 7 | (uint)this.Height >> 25));
            return x;
        }

        public void Inflate(float x, float y)
        {
            this.X = this.X - x;
            this.Y = this.Y - y;
            this.Width = this.Width + 2f * x;
            this.Height = this.Height + 2f * y;
        }

        public void Inflate(SizeF size)
        {
            this.Inflate(size.Width, size.Height);
        }

        public static RectangleF Inflate(RectangleF rect, float x, float y)
        {
            RectangleF rectangleF = rect;
            rectangleF.Inflate(x, y);
            return rectangleF;
        }

        public void Intersect(RectangleF rect)
        {
            RectangleF rectangleF = RectangleF.Intersect(rect, this);
            this.X = rectangleF.X;
            this.Y = rectangleF.Y;
            this.Width = rectangleF.Width;
            this.Height = rectangleF.Height;
        }

        public static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            RectangleF rectangleF;
            float single = Math.Max(a.X, b.X);
            float single1 = Math.Min(a.X + a.Width, b.X + b.Width);
            float single2 = Math.Max(a.Y, b.Y);
            float single3 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            rectangleF = ((single1 < single ? true : single3 < single2) ? RectangleF.Empty : new RectangleF(single, single2, single1 - single, single3 - single2));
            return rectangleF;
        }

        public bool IntersectsWith(RectangleF rect)
        {
            return (rect.X >= this.X + this.Width || this.X >= rect.X + rect.Width || rect.Y >= this.Y + this.Height ? false : this.Y < rect.Y + rect.Height);
        }

        public void Offset(PointF pos)
        {
            this.Offset(pos.X, pos.Y);
        }

        public void Offset(float x, float y)
        {
            this.X = this.X + x;
            this.Y = this.Y + y;
        }

        public static bool operator ==(RectangleF left, RectangleF right)
        {
            return (left.X != right.X || left.Y != right.Y || left.Width != right.Width ? false : left.Height == right.Height);
        }

        public static implicit operator RectangleF(Rectangle r)
        {
            RectangleF rectangleF = new RectangleF((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);
            return rectangleF;
        }

        public static bool operator !=(RectangleF left, RectangleF right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            string[] str = new string[] { "{X=", null, null, null, null, null, null, null, null };
            float x = this.X;
            str[1] = x.ToString(CultureInfo.CurrentCulture);
            str[2] = ",Y=";
            x = this.Y;
            str[3] = x.ToString(CultureInfo.CurrentCulture);
            str[4] = ",Width=";
            x = this.Width;
            str[5] = x.ToString(CultureInfo.CurrentCulture);
            str[6] = ",Height=";
            x = this.Height;
            str[7] = x.ToString(CultureInfo.CurrentCulture);
            str[8] = "}";
            return string.Concat(str);
        }

        public static RectangleF Union(RectangleF a, RectangleF b)
        {
            float single = Math.Min(a.X, b.X);
            float single1 = Math.Max(a.X + a.Width, b.X + b.Width);
            float single2 = Math.Min(a.Y, b.Y);
            float single3 = Math.Max(a.Y + a.Height, b.Y + b.Height);
            RectangleF rectangleF = new RectangleF(single, single2, single1 - single, single3 - single2);
            return rectangleF;
        }
    }
}
