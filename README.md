# DOTS Dispatcher

DOTS Dispatcher is a simple yet performant entity as event system to be used with Unity's DOTS ECS.

Entities are created with the corresponding data and will live for one frame.

Since the events are entities, they are fully compatible with the Entities API and they may be queried whatever way suits you best.

### Usage

DOTS Dispatcher provides an abstract `DispatcherSystem`.

The `DispatcherSystem` must be inherited by a new class that must be running in the `SystemGroup` of your choice.

Once the system is created, multiple `NativeQueue<T>` can be created through the system API method `CreateDispatcherQueue<T>()` and populated with the desired events data.

Events will be created when the corresponding `DispatcherSystem` runs. 

Depending of the execution order they will be created with a one frame delay, and it's important to have this detail in mind when designing your data flow.

### Examples

* DispatcherSystem Declaration
```
public class SimulationDispatcherSystem : DispatcherSystem
{
}
```

* Event Data Declaration
```
public struct ZeroSizeTestData : IComponentData
{
}

public struct TestData : IComponentData
{
    public int Value;
}
```

* Produce Event
```
public class EventProducerSystem : SystemBase
{
    DispatcherSystem _dispatcherSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        _dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
    }

    protected override void OnUpdate()
    {
        var dispatcherQueue = _dispatcherSystem.CreateDispatcherQueue<ZeroSizeTestData>();
        
        for (var index = 0; index < 0xFF; index++)
        {
            dispatcherQueue.Enqueue(default);
        }
    }
}

public class EventProducerJobSystem : SystemBase
{
    DispatcherSystem _dispatcherSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        _dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
    }

    protected override void OnUpdate()
    {
        // DispatcherQueue is a NativeQueue<T>, thus, fully supported by jobs.
        var dispatcherQueue = _dispatcherSystem.CreateDispatcherQueue<TestData>().AsParallelWriter();
        
        // Just and example on how to create events in jobs.
        Entities
            .WithAll<ZeroSizeTestData>()
            .ForEach((Entity entity) => 
            {
                dispatcherQueue.Enqueue(new TestData { Value = entity.Index });
            })
            .ScheduleParallel();
        
        // Since this system is producing events in Jobs, i.e. asynchronously, 
        // it must be declared as a dependency for the DispatcherSystem through this API call.
        _dispatcherSystem.AddJobHandleForProducer(Dependency);
    }
}
```

* Consume Events
```
public class EventConsumerSystem : SystemBase
{
    EntityQuery _zeroSizeTestDataQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        _zeroSizeTestDataQuery = GetEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>());
    }

    protected override void OnUpdate()
    {
        // Examples. Query the entity events as you please.
    
        Debug.Log(_zeroSizeTestDataQuery.CalculateEntityCount());
        
        Entities
            .ForEach((TestData testData) => 
            {
                // Do whatever you want with it.
            })
            .ScheduleParallel();
    }
}
```
