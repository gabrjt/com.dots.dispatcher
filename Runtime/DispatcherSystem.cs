using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.Dispatcher.Runtime
{
    [AlwaysUpdateSystem]
    public abstract class DispatcherSystem : SystemBase
    {
        readonly Dictionary<Type, IDispatcherContainer> _dictionary =
            new Dictionary<Type, IDispatcherContainer>();

        /// <summary>
        /// Creates a NativeQueue which can be used to enqueue Event Data to be created when the DispatcherSystem runs.
        /// </summary>
        /// <typeparam name="T">struct, IComponentData</typeparam>
        /// <returns>NativeQueue</returns>
        public NativeQueue<T> CreateDispatcherQueue<T>() where T : struct, IComponentData
        {
            if (!_dictionary.TryGetValue(typeof(T), out var dispatcherContainer))
            {
                _dictionary.Add(typeof(T), dispatcherContainer = new DispatcherContainer<T>(this));
            }

            return ((DispatcherContainer<T>) dispatcherContainer).CreateDispatcherQueue();
        }

        /// <summary>
        /// Must be used if the DispatcherQueue is enqueueing data in Jobs, i.e. asynchronously.
        /// Guarantees that the DispatcherSystem will only run after its dependencies,
        /// which are determined by this function call.
        /// </summary>
        /// <param name="dependency">The event producer system dependency</param>
        public void AddJobHandleForProducer(JobHandle dependency)
        {
            Dependency = JobHandle.CombineDependencies(Dependency, dependency);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            foreach (var dispatcherContainer in _dictionary.Values)
            {
                dispatcherContainer.Dispose();
            }

            _dictionary.Clear();
        }

        protected override void OnUpdate()
        {
            if (_dictionary.Count == 0)
            {
                return;
            }

            foreach (var dispatcherContainer in _dictionary.Values)
            {
                dispatcherContainer.Update();
            }
        }


        interface IDispatcherContainer : IDisposable
        {
            void Update();
        }

        class DispatcherContainer<T> : IDispatcherContainer where T : struct, IComponentData
        {
            readonly EntityArchetype _archetype;
            readonly EntityManager _entityManager;
            readonly bool _isZeroSized;
            readonly EntityQuery _query;
            readonly List<NativeQueue<T>> _queueList;

            public DispatcherContainer(DispatcherSystem dispatcherSystem)
            {
                var componentType = ComponentType.ReadWrite<T>();

                _entityManager = dispatcherSystem.EntityManager;
                _query = dispatcherSystem.GetEntityQuery(componentType);
                _archetype = _entityManager.CreateArchetype(componentType);
                _isZeroSized = componentType.IsZeroSized;
                _queueList = new List<NativeQueue<T>>();
            }

            public void Update()
            {
                _entityManager.DestroyEntity(_query);

                var entityCount = 0;

                if (_isZeroSized)
                {
                    foreach (var queue in _queueList)
                    {
                        unsafe
                        {
                            new SumZeroSizedEntityCountJob
                                {
                                    EntityCount = &entityCount,
                                    Queue = queue
                                }
                                .Run();
                        }

                        queue.Dispose();
                    }

                    _entityManager.CreateEntity(_archetype, entityCount);
                }
                else
                {
                    var list = new NativeList<T>(Allocator.TempJob);

                    foreach (var queue in _queueList)
                    {
                        unsafe
                        {
                            new SumEntityCountJob
                                {
                                    EntityCount = &entityCount,
                                    Queue = queue,
                                    List = list
                                }
                                .Run();
                        }

                        queue.Dispose();
                    }

                    _entityManager.CreateEntity(_archetype, entityCount);

                    _query.CopyFromComponentDataArrayAsync(list.AsArray(), out var jobHandle);

                    list.Dispose(jobHandle);

                    _query.AddDependency(jobHandle);
                }

                _queueList.Clear();
            }

            public void Dispose()
            {
                _query.CompleteDependency();

                foreach (var queue in _queueList)
                {
                    queue.Dispose();
                }

                _queueList.Clear();
            }

            public NativeQueue<T> CreateDispatcherQueue(Allocator allocator = Allocator.TempJob)
            {
                var queue = new NativeQueue<T>(allocator);

                _queueList.Add(queue);

                return queue;
            }

            [BurstCompile]
            unsafe struct SumZeroSizedEntityCountJob : IJob
            {
                [NativeDisableUnsafePtrRestriction] public int* EntityCount;
                [ReadOnly] public NativeQueue<T> Queue;

                public void Execute()
                {
                    *EntityCount += Queue.Count;
                }
            }

            [BurstCompile]
            unsafe struct SumEntityCountJob : IJob
            {
                [NativeDisableUnsafePtrRestriction] public int* EntityCount;
                [ReadOnly] public NativeQueue<T> Queue;
                public NativeList<T> List;

                public void Execute()
                {
                    *EntityCount += Queue.Count;

                    List.AddRange(Queue.ToArray(Allocator.Temp));
                }
            }
        }
    }
}