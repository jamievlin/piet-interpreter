using System;
using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;

namespace piet
{
    using PixelPoint = Tuple<int, int>;
    class RangeVal<T> where T : IComparable<T>
    {
        private T min, max;

        bool _initialized;
        public bool ValueInitalized { get => _initialized; }

        public T Min { get => min; }
        public T Max { get => max; }

        public RangeVal()
        {
            _initialized = false;
        }

        public RangeVal(T val)
        {
            _initialized = true;
            min = val;
            max = val;
        }

        public Tuple<bool, bool> AddValue(T val)
        {
            if (!ValueInitalized)
            {
                min = val;
                max = val;
                _initialized = true;
            }

            bool changeleft = false;
            bool changeright = false;

            if (val.CompareTo(min) < 0)
            {
                changeleft = true;
                min = val;
            }

            if (val.CompareTo(max) > 0)
            {
                changeright = true;
                max = val;
            }

            return new Tuple<bool, bool>(changeleft, changeright);
        }
    }

    public class PietColorBlock
    {
        protected static Tuple<int, int> ColorToHue(uint color) => color switch
        {
            0xFFC0C0 => new(0, 0),
            0xFF0000 => new(0, 1),
            0xC00000 => new(0, 2),

            0xFFFFC0 => new(1, 0),
            0xFFFF00 => new(1, 1),
            0xC0C000 => new(1, 2),

            0xC0FFC0 => new(2, 0),
            0x00FF00 => new(2, 1),
            0x00C000 => new(2, 2),

            0xC0FFFF => new(3, 0),
            0x00FFFF => new(3, 1),
            0x00C0C0 => new(3, 2),

            0xC0C0FF => new(4, 0),
            0x0000FF => new(4, 1),
            0x0000C0 => new(4, 2),

            0xFFC0FF => new(5, 0),
            0xFF00FF => new(5, 1),
            0xC000C0 => new(5, 2),

            0xFFFFFF => new(-1, 0),
            _ => new(-1, -1)
        };

        public PixelPoint nextPixel(Direction dp, CodelChooser cc) => (dp, cc) switch
        {
            (Direction.Right, CodelChooser.Left) => new PixelPoint(leftright.Max, right.Min),
            (Direction.Right, CodelChooser.Right) => new PixelPoint(leftright.Max, right.Max),

            (Direction.Down, CodelChooser.Left) => new PixelPoint(bottom.Max, topbtm.Max),
            (Direction.Down, CodelChooser.Right) => new PixelPoint(bottom.Min, topbtm.Max),

            (Direction.Left, CodelChooser.Left) => new PixelPoint(leftright.Min, left.Max),
            (Direction.Left, CodelChooser.Right) => new PixelPoint(leftright.Min, left.Min),

            (Direction.Up, CodelChooser.Left) => new PixelPoint(top.Min, topbtm.Min),
            (Direction.Up, CodelChooser.Right) => new PixelPoint(top.Max, topbtm.Min),

            _ => throw new ArgumentOutOfRangeException("Invalid (dp, cc) pair.")
        };

        public PixelPoint nextPixel(PixelPoint origPx, Direction dp, CodelChooser cc) =>
            color == new PixelPoint(-1, 0) ? origPx : nextPixel(dp, cc);

        private readonly HashSet<PixelPoint> pixels;
        private readonly Tuple<int, int> color;
        public Tuple<int, int> Color { get => color; }

        private readonly RangeVal<int> topbtm;
        private readonly RangeVal<int> leftright;
        private RangeVal<int> top, bottom, left, right;

        public int CodelValue { get => pixels.Count / 25; }

        public PietColorBlock(Byte4 color)
        {
            pixels = new HashSet<PixelPoint>();

            uint testValue = color.PackedValue & 0x00FFFFFF;
            uint red = testValue & 0xFF;
            uint blue = testValue & 0xFF0000;
            uint green = testValue & 0xFF00;

            uint testVal = (red << 16) | green | (blue >> 16);
            this.color = ColorToHue(testVal);

            topbtm = new RangeVal<int>();
            leftright = new RangeVal<int>();

            top = new RangeVal<int>();
            bottom = new RangeVal<int>();
            left = new RangeVal<int>();
            right = new RangeVal<int>();
        }

        public void AddPixel(PixelPoint pt)
        {
            pixels.Add(pt);
            var x = pt.Item1;
            var y = pt.Item2;

            if (topbtm.ValueInitalized)
            {
                if (y == topbtm.Min)
                {
                    top.AddValue(x);
                }
                if (y == topbtm.Max)
                {
                    bottom.AddValue(x);
                }
            }

            if (leftright.ValueInitalized)
            {
                if (x == leftright.Min)
                {
                    left.AddValue(y);
                }
                if (x == leftright.Max)
                {
                    right.AddValue(y);
                }
            }

            var result_tb = topbtm.AddValue(y);
            var result_lr = leftright.AddValue(x);

            if (result_lr.Item1)
            {
                left = new(y);
            }
            if (result_lr.Item2)
            {
                right = new(y);
            }

            if (result_tb.Item1)
            {
                top = new(x);
            }
            if (result_tb.Item2)
            {
                bottom = new(x);
            }
        }


    }
}
