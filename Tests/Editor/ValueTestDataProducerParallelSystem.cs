using DOTS.Dispatcher.Runtime;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.Dispatcher.Tests.Editor
{
    [DisableAutoCreation]
    internal class ValueTestDataProducerParallelSystem : SystemBase
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

            var dispatcherQueue = _dispatcherSystem.CreateDispatcherQueue<ValueTestData>();

            Dependency = new ProducerJob
                {
                    Queue = dispatcherQueue.AsParallelWriter()
                }
                .Schedule(Count, 64, Dependency);

            _dispatcherSystem.AddJobHandleForProducer(Dependency);
        }

        [BurstCompile]
        struct ProducerJob : IJobParallelFor
        {
            public NativeQueue<ValueTestData>.ParallelWriter Queue;

            public void Execute(int index)
            {
                Queue.Enqueue(new ValueTestData(index));
            }
        }
    }
}