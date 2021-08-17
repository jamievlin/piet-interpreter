using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace piet
{
    using PixelPoint = Tuple<int, int>;
    public class PietProgram
    {
        private Image<Byte4> image;
        private List<PietColorBlock> pietColorBlocks;
        private int[,] pixelCountMap;

        protected int CodelWidth { get; private set; }
        protected int CodelHeight { get; private set; }

        protected List<PietColorBlock> PietColorBlocks { get => pietColorBlocks; }
        protected int[,] PixelCountMap { get => pixelCountMap; }
        protected Image<Byte4> Img { get => image; }

        public PietProgram(string fileName, int codelWidth=1, int codelHeight=1)
        {
            image = Image.Load<Byte4>(fileName);
            pietColorBlocks = new();
            pixelCountMap = new int[image.Width, image.Height];
            CodelWidth = codelWidth;
            CodelHeight = codelHeight;
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

    }
}
