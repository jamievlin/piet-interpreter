using System;

namespace piet
{
    class Program
    {
        static void Main(string[] args)
        {
            PietProgram pietProgram = new PietProgram("Piet_hello_big.png");
            pietProgram.constructBlocks();
            pietProgram.execute();
        }
    }
}
