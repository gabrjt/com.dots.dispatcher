using DOTS.Dispatcher.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.Dispatcher.Tests.Editor
{
    [DisableAutoCreation]
    internal class ZeroSizeTestDataProducerParallelSystem : SystemBase
    {
        DispatcherSystem _dispatcherSystem;
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

            Dependency = new ProducerJob
                {
                    Queue = dispatcherQueue.AsParallelWriter()
                }
                .Schedule(count, 64, Dependency);

            _dispatcherSystem.AddJobHandleForProducer(Dependency);
        }

        [BurstCompile]
        private struct ProducerJob : IJobParallelFor
        {
            public NativeQueue<ZeroSizeTestData>.ParallelWriter Queue;

            public void Execute(int index)
            {
                Queue.Enqueue(default);
            }
        }
    }
}