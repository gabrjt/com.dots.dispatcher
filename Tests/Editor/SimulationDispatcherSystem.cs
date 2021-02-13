using DOTS.Dispatcher.Runtime;
using Unity.Entities;

namespace DOTS.Dispatcher.Tests.Editor
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true, OrderLast = false)]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    internal class SimulationDispatcherSystem : DispatcherSystem
    {
    }
}