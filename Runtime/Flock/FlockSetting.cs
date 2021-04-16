namespace Triniti.Flock
{
    public struct FlockSetting
    {
        public float CellRadius;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float CohesionWeight;
        public float GuideWeight;

        public FlockSetting(float cellRadius = 10)
        {
            CellRadius = cellRadius;
            SeparationWeight = 1;
            AlignmentWeight = 1;
            CohesionWeight = 1f;
            GuideWeight = 1;
            _Normalize();
        }

        private void _Normalize()
        {
            var sum = SeparationWeight + AlignmentWeight + CohesionWeight + GuideWeight;
            SeparationWeight /= sum;
            AlignmentWeight /= sum;
            CohesionWeight /= sum;
            GuideWeight /= sum;
        }
    }
}