using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock.FlowField
{
    public struct CostCell
    {
        public const byte IMPASSABLE = 255;
        //public int Index; //flattened index x*width + y
        public byte Value; //pass cost (255 is impassible)
    }

    public enum CellDirection
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest,
        None,
    }

    public struct FlowFieldCell
    {
        //public int Index; //flattened index x*width + y
        public float BestCost; //calculated by flow field builder
        public int2 BestDirection;
        public byte Direction;
        public const byte NO_DIRECTION = 255;
    }
}