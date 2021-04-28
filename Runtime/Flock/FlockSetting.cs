namespace Triniti.Flock
{
    public struct FlockSetting
    {
        public float CellRadius;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float CohesionWeight;

        public FlockSetting(float cellRadius = 10)
        {
            CellRadius = cellRadius;
            SeparationWeight = 0.5f;
            AlignmentWeight = 0;
            CohesionWeight = 0;
        }
    }
}