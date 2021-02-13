using DOTS.Dispatcher.Runtime;
using Unity.Entities;

namespace DOTS.Dispatcher.Tests.Editor
{
    [DisableAutoCreation]
    internal class ZeroSizeTestDataProducerSingleSystem : SystemBase
    {
        DispatcherSystem _dispatcherSystem;
        public bool AddJobHandleForProducer = true;
        public int Count;

        protected override void OnCreate()
        {
            base.OnCreate();

            _dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
        }

        protected override void OnUpdate()
        {
            if (Count == 0)
            {
                return;
            }

            var dispatcherQueue = _dispatcherSystem.CreateDispatcherQueue<ZeroSizeTestData>();
            var count = Count;

            Job
                .WithCode(
                    () =>
                    {
                        for (var index = 0; index < count; index++)
                        {
                            dispatcherQueue.Enqueue(default);
                        }
                    }
                )
                .Run();

            if (AddJobHandleForProducer)
            {
                _dispatcherSystem.AddJobHandleForProducer(Dependency);
            }
        }
    }
}