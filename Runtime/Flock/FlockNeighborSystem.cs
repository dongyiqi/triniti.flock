using Unity.Entities;

namespace Triniti.Flock
{
    [UpdateInGroup(typeof(FlockGroup)), UpdateBefore(typeof(FlockSystem))]
    public class FlockNeighborBrutForceSystem : SystemBase
    {
        protected override void OnUpdate()
        {
        }
    }
}