using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace piet
{
    using PixelPoint = Tuple<int, int>;

    class RangeVal<T> where T : IComparable<T>
    {
        private T min, max;

        bool _initialized;
        public bool ValueInitalized { get => _initialized;  }

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

        public Tuple<bool,bool> AddValue(T val)
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

    class PietColorBlock
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
    public class PietProgram
    {
        private Image<Byte4> image;
        private List<PietColorBlock> pietColorBlocks;
        private int[,] pixelCountMap;

        private CodelChooser cc;
        private Direction dp;
        private PixelPoint codel;

        private Stack<int> progStack;
        bool active = true;

        public PietProgram(string fileName)
        {
            image = Image.Load<Byte4>(fileName);
            pietColorBlocks = new();
            pixelCountMap = new int[image.Width, image.Height];
            progStack = new();
        }


        public void constructBlocks()
        {
            pietColorBlocks.Clear();
            var neighbors = new HashSet<PixelPoint>();
            var greyOrBlack = new HashSet<PixelPoint>();
            PixelPoint start = new(0, 0);

            while (1 == 1)
            {
                Queue<PixelPoint> proc = new();
                proc.Enqueue(start);
                var newColBlock = new PietColorBlock(image[start.Item1, start.Item2]);
                neighbors.Remove(start);
                greyOrBlack.Add(start);

                while (proc.Count > 0)
                {
                    var px = proc.Dequeue();
                    var x = px.Item1;
                    var y = px.Item2;

                    newColBlock.AddPixel(px);
                    pixelCountMap[x, y] = pietColorBlocks.Count;

                    var color = image[x, y];

                    var test_pxs = new List<PixelPoint> {
                        new PixelPoint(x-1, y), new PixelPoint(x+1, y),
                        new PixelPoint(x, y+1), new PixelPoint(x, y-1)
                    };

                    foreach(PixelPoint ptx in test_pxs)
                    {
                        var px_ = ptx.Item1;
                        var py_ = ptx.Item2;

                        bool valid_range = 0 <= px_ && px_ < image.Width && 0 <= py_ && py_ < image.Height;
                        if (valid_range && !greyOrBlack.Contains(ptx))
                        {
                            if (color == image[px_, py_])
                            {
                                neighbors.Remove(ptx);
                                greyOrBlack.Add(ptx);
                                proc.Enqueue(ptx);
                            }
                            else
                            {
                                neighbors.Add(ptx);
                            }
                        }
                    }
                }
                pietColorBlocks.Add(newColBlock);

                if (neighbors.Count > 0)
                {
                    start = neighbors.First();
                }
                else
                {
                    break;
                }
            }
        }

        public void resetMachine()
        {
            progStack.Clear();
            codel = new PixelPoint(0, 0);
            
            cc = CodelChooser.Left;
            dp = Direction.Right;
            active = true;
        }

        public void step()
        {
            var oldCdlIndex = pixelCountMap[codel.Item1, codel.Item2];
            var oldCdl = pietColorBlocks[oldCdlIndex];

            PixelPoint nextPt = getNextPxNonWhite();
            if (!active)
            {
                return;
            }

            var cdlIndex = pixelCountMap[nextPt.Item1, nextPt.Item2];
            var cdl = pietColorBlocks[cdlIndex];



            if (cdl.Color.Item1 == -1 && cdl.Color.Item2 == -1)
            {
                throw new Exception("Reached a black block");
            }

            var hueDiff = (cdl.Color.Item1 - oldCdl.Color.Item1) % 6 ;
            hueDiff = (hueDiff + 6) % 6;

            var lightDiff = (cdl.Color.Item2 - oldCdl.Color.Item2) % 3;
            lightDiff = (lightDiff + 3) % 3;

#if DEBUG
            // Console.WriteLine("Step: {0},{1} -> {2},{3}", codel.Item1, codel.Item2, nextPt.Item1, nextPt.Item2);
            // Console.WriteLine("Diff: {0}, {1}", hueDiff, lightDiff);
#endif 
            switch (hueDiff, lightDiff)
            {
                case (0, 0):
                    break;
                case (0, 1):
                    {
                        var a = oldCdl.CodelValue;
                        progStack.Push(a);
                    }
                    break;
                case (0, 2):
                    progStack.Pop();
                    break;
                case (1, 0):
                    {
                        var a = progStack.Pop();
                        var b = progStack.Pop();
                        progStack.Push(a + b);
                    }
                    break;
                case (1, 1):
                    {
                        var a = progStack.Pop();
                        var b = progStack.Pop();
                        progStack.Push(a - b);
                    }
                    break;
                case (1, 2):
                    {
                        var a = progStack.Pop();
                        var b = progStack.Pop();
                        progStack.Push(a * b);
                    }
                    break;
                case (2, 0):
                    {
                        var a = progStack.Pop();
                        var b = progStack.Pop();
                        progStack.Push(a / b);
                    }
                    break;
                case (2, 1):
                    {
                        var a = progStack.Pop();
                        var b = progStack.Pop();
                        progStack.Push(((a % b) + b) % b);
                    }
                    break;
                case (2, 2):
                    {
                        var a = progStack.Pop();
                        progStack.Push(a == 0 ? 1 : 0);
                    }
                    break;
                case (3, 0):
                    {
                        var a = progStack.Pop();
                        var b = progStack.Pop();
                        progStack.Push(b > a ? 1 : 0);
                    }
                    break;
                case (3, 1):
                    {
                        dp = PtrFns.clockwise(dp);
                    }
                    break;
                case (3, 2):
                    cc = PtrFns.switchFn(cc);
                    break;
                case (4, 0):
                    {
                        var a = progStack.Peek();
                        progStack.Push(a);
                    }
                    break;
                case (4, 1):
                    {
                        var a = progStack.Peek();
                        progStack.Push(a);
                    }
                    break;
                case (4, 2):
                    {
                        int a;
                        if (int.TryParse(Console.ReadLine(), out a))
                        {
                            progStack.Push(a);
                        }
                    }
                    break;
                case (5, 0):
                    {
                        progStack.Push(Console.Read());
                    }
                    break;
                case (5, 1):
                    {
                        var a = progStack.Pop();
                        Console.Write("{0}", a);
                    }
                    break;
                case (5, 2):
                    {
                        var a = progStack.Pop();
                        Console.Write("{0}", (char)a);
                    }
                    break;
                default:
                    throw new Exception("Should not happen");
            }

            codel = nextPt;
        }

        public PixelPoint getNextPxNonWhite()
        {
            PixelPoint nextPx;
            var cdlIndex = pixelCountMap[codel.Item1, codel.Item2];
            var cdl = pietColorBlocks[cdlIndex];
            int attempts = 8;
            bool validNextCdl;
            do
            {
                PixelPoint endBlock = cdl.nextPixel(dp, cc);

                var ex = endBlock.Item1;
                var ey = endBlock.Item2;
                nextPx = dp switch
                {
                    Direction.Left => new(ex - 1, ey),
                    Direction.Right => new(ex + 1, ey),
                    Direction.Up => new(ex, ey - 1),
                    Direction.Down => new(ex, ey + 1),
                };

                validNextCdl = blackOrEdge(nextPx);
                if (!validNextCdl)
                {
                    return nextPx;
                }

                // if not validNextCdl;
                if (attempts > 0)
                {
                    attempts -= 1;
                    if (attempts % 2 == 0)
                    {
                        dp = PtrFns.clockwise(dp);
                    }
                    else
                    {
                        cc = PtrFns.switchFn(cc);
                    }
                }
            } while (attempts > 0);
            active = false;
            return codel;
        }

        public bool blackOrEdge(PixelPoint px)
        {
            bool inRange =
                0 <= px.Item1 && px.Item1 < image.Width &&
                0 <= px.Item2 && px.Item2 < image.Height;

            if (!inRange)
            {
                return true;
            }
            else
            {
                var codelIdx = pixelCountMap[px.Item1, px.Item2];
                return pietColorBlocks[codelIdx].Color.Equals(new Tuple<int, int>(-1, -1));
            }
        }

        public void execute()
        {
            if (pietColorBlocks == null)
            {
                constructBlocks();
            }

            resetMachine();

            // step
            while (active)
            {
                step();
            }
        }
    }
}
