namespace Triniti.Flock
{
    public struct FlockSetting
    {
        public float CellRadius;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float CohesionWeight;
        public float SteerWeight;

        public FlockSetting(float cellRadius = 10)
        {
            CellRadius = cellRadius;
            SeparationWeight = 1;
            AlignmentWeight = 1;
            CohesionWeight = 1f;
            SteerWeight = 1;
        }
    }
}