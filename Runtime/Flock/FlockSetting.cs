namespace Triniti.Flock
{
    public struct FlockSetting
    {
        public float CellRadius;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float GuideWeight;

        public FlockSetting(float cellRadius = 10)
        {
            CellRadius = cellRadius;
            SeparationWeight = 1;
            AlignmentWeight = 1;
            GuideWeight = 1;
        }
    }
}