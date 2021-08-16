using System;

namespace piet
{
    public enum Direction
    {
        Right=1, Down=2, Left=3, Up=0
    }

    public enum CodelChooser
    {
        Left=0, Right=1
    }

    public class PtrFns
    {
        public static Direction clockwise(Direction d) => d switch
        {
            Direction.Left => Direction.Up,
            Direction.Right => Direction.Down,
            Direction.Up => Direction.Right,
            Direction.Down => Direction.Left,
            _ => throw new Exception("Invalid direciton pointer")
        };

        public static CodelChooser switchFn(CodelChooser cc) => cc switch
        {
            CodelChooser.Left => CodelChooser.Right,
            CodelChooser.Right => CodelChooser.Left,
            _ => throw new Exception("Invalid CodelChooser")
        };
    }
}