using System;

namespace piet
{
    class Program
    {
        static void Main(string[] args)
        {
            PietEngine pietProgram = new PietEngine("Piet_hello_big.png");
            pietProgram.constructBlocks();
            pietProgram.execute();
        }
    }
}
