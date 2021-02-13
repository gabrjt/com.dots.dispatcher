using Unity.Entities;

namespace DOTS.Dispatcher.Tests.Editor
{
    [DisableAutoCreation]
    internal class ValueTestDataConsumerSystem : SystemBase
    {
        EntityQuery _query;

        public EntityQuery Query => _query;

        public int Count { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            _query = GetEntityQuery(ComponentType.ReadOnly<ValueTestData>());
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            Count = 0;
        }

        protected override void OnUpdate()
        {
            Count = _query.CalculateEntityCount();

            Entities
                .ForEach(
                    (in ValueTestData testData) => { }
                )
                .ScheduleParallel();
        }
    }
}