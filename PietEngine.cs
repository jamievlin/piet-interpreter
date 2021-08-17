using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace piet
{
    using PixelPoint = Tuple<int, int>;

    public class PietEngine : PietProgram
    {
        private CodelChooser cc;
        private Direction dp;
        private PixelPoint codel;

        private Stack<int> progStack;
        bool active = true;

        public PietEngine(string fileName) : base(fileName)
        {
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
            var oldCdlIndex = PixelCountMap[codel.Item1, codel.Item2];
            var oldCdl = PietColorBlocks[oldCdlIndex];

            PixelPoint nextPt = getNextPxNonWhite();
            if (!active)
            {
                return;
            }

            var cdlIndex = PixelCountMap[nextPt.Item1, nextPt.Item2];
            var cdl = PietColorBlocks[cdlIndex];



            if (cdl.Color.Item1 == -1 && cdl.Color.Item2 == -1)
            {
                throw new Exception("Reached a black block");
            }

            var hueDiff = (cdl.Color.Item1 - oldCdl.Color.Item1) % 6;
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
                        var a = progStack.Pop();
                        a = ((a % 4) + a) % 4;
                        for (int i = 0; i < a; ++i)
                        {
                            dp = PtrFns.clockwise(dp);
                        }
                    }
                    break;
                case (3, 2):
                    {
                        var a = progStack.Pop();
                        if (a % 2 != 0)
                            cc = PtrFns.switchFn(cc);
                    }
                    break;
                case (4, 0):
                    {
                        var a = progStack.Peek();
                        progStack.Push(a);
                    }
                    break;
                case (4, 1):
                    {
                        var nrolls = progStack.Peek();
                        var depth = progStack.Peek();

                        var topdepth = ((nrolls % depth) + nrolls) % depth;
                        if (depth > 1 && nrolls > 0)
                        {
                            Stack<int> rollbk = new();
                            Stack<int> rest = new();
                            for (int i = 0; i < topdepth; ++i)
                            {
                                rollbk.Push(progStack.Pop());
                            }
                            for (int j = 0; j < depth - topdepth; ++j)
                            {
                                rest.Push(progStack.Pop());
                            }

                            while (rollbk.Count > 0)
                            {
                                progStack.Push(rollbk.Pop());
                            }
                            while (rest.Count > 0)
                            {
                                progStack.Push(rest.Pop());
                            }
                        }
                    }
                    break;
                case (4, 2):
                    {
                        if (int.TryParse(Console.ReadLine(), out int a))
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
            var cdlIndex = PixelCountMap[codel.Item1, codel.Item2];
            var cdl = PietColorBlocks[cdlIndex];
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
                0 <= px.Item1 && px.Item1 < Img.Width &&
                0 <= px.Item2 && px.Item2 < Img.Height;

            if (!inRange)
            {
                return true;
            }
            else
            {
                var codelIdx = PixelCountMap[px.Item1, px.Item2];
                return PietColorBlocks[codelIdx].Color.Equals(new Tuple<int, int>(-1, -1));
            }
        }

        public void execute()
        {
            if (PietColorBlocks == null)
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
