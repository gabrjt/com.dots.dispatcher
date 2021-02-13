using System.Diagnostics;
using System.Threading;
using DOTS.Extensions.Tests.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Debug = UnityEngine.Debug;

namespace DOTS.Dispatcher.Tests.Editor
{
    public class DispatcherSystemTests : ECSTestFixture
    {
        public override void Setup()
        {
            base.Setup();

            var simulationDispatcherSystem = new SimulationDispatcherSystem();
            var zeroSizeTestDataProducerSingleSystem = new ZeroSizeTestDataProducerSingleSystem();
            var zeroSizeTestDataProducerParallelSystem = new ZeroSizeTestDataProducerParallelSystem();
            var zeroSizeTestDataConsumerSystem = new ZeroSizeTestDataConsumerSystem();
            var testDataProducerSingleSystem = new ValueTestDataProducerSingleSystem();
            var testDataProducerParallelSystem = new ValueTestDataProducerParallelSystem();
            var testDataConsumerSystem = new ValueTestDataConsumerSystem();

            World.AddSystem(simulationDispatcherSystem);
            World.AddSystem(zeroSizeTestDataProducerSingleSystem);
            World.AddSystem(zeroSizeTestDataProducerParallelSystem);
            World.AddSystem(zeroSizeTestDataConsumerSystem);
            World.AddSystem(testDataProducerSingleSystem);
            World.AddSystem(testDataProducerParallelSystem);
            World.AddSystem(testDataConsumerSystem);

            var simulationSystemGroup = World.GetExistingSystem<SimulationSystemGroup>();

            Assert.IsNotNull(simulationSystemGroup);

            simulationSystemGroup.AddSystemToUpdateList(simulationDispatcherSystem);
            simulationSystemGroup.AddSystemToUpdateList(zeroSizeTestDataProducerSingleSystem);
            simulationSystemGroup.AddSystemToUpdateList(zeroSizeTestDataProducerParallelSystem);
            simulationSystemGroup.AddSystemToUpdateList(zeroSizeTestDataConsumerSystem);
            simulationSystemGroup.AddSystemToUpdateList(testDataProducerSingleSystem);
            simulationSystemGroup.AddSystemToUpdateList(testDataProducerParallelSystem);
            simulationSystemGroup.AddSystemToUpdateList(testDataConsumerSystem);
        }

        [Test]
        public void TestCreateDispatcherQueue()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            var zeroSizeTestDataDispatcherQueue = dispatcherSystem.CreateDispatcherQueue<ZeroSizeTestData>();

            Assert.IsTrue(zeroSizeTestDataDispatcherQueue.IsCreated);
            Assert.AreEqual(zeroSizeTestDataDispatcherQueue.Count, 0);

            zeroSizeTestDataDispatcherQueue.Enqueue(default);

            Assert.AreEqual(zeroSizeTestDataDispatcherQueue.Count, 1);

            var testDataDispatcherQueue = dispatcherSystem.CreateDispatcherQueue<ValueTestData>();

            Assert.IsTrue(testDataDispatcherQueue.IsCreated);
            Assert.AreEqual(testDataDispatcherQueue.Count, 0);

            testDataDispatcherQueue.Enqueue(new ValueTestData(1));

            Assert.AreEqual(testDataDispatcherQueue.Count, 1);

            var zeroSizeTestDataQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>());
            var testDataQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ValueTestData>());

            Assert.AreEqual(zeroSizeTestDataQuery.CalculateEntityCount(), 0);
            Assert.AreEqual(testDataQuery.CalculateEntityCount(), 0);

            dispatcherSystem.Update();

            Assert.AreEqual(zeroSizeTestDataQuery.CalculateEntityCount(), 1);
            Assert.AreEqual(testDataQuery.CalculateEntityCount(), 1);
        }

        [Test]
        public void TestZeroSizeTestDataProducerSingleSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ZeroSizeTestDataProducerSingleSystem>();

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSystem);
            Assert.IsTrue(producerSystem.ShouldRunSystem());
            Assert.IsTrue(producerSystem.Enabled);

            var count = 0xFFFF;

            producerSystem.Count = count;

            var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>());

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), 0);

            producerSystem.Count = 0;

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), count);

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), 0);
        }

        [Test]
        public void TestZeroSizeTestDataProducerParallelSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ZeroSizeTestDataProducerParallelSystem>();

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSystem);
            Assert.IsTrue(producerSystem.ShouldRunSystem());
            Assert.IsTrue(producerSystem.Enabled);

            var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>());
            var count = 0xFFFF;

            producerSystem.Count = count;

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), 0);

            producerSystem.Count = 0;

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), count);

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), 0);
        }

        [Test]
        public void TestZeroSizeTestDataConsumerSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSingleSystem = World.GetExistingSystem<ZeroSizeTestDataProducerSingleSystem>();
            var producerParallelSystem = World.GetExistingSystem<ZeroSizeTestDataProducerParallelSystem>();
            var consumerSystem = World.GetExistingSystem<ZeroSizeTestDataConsumerSystem>();

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSingleSystem);
            Assert.IsTrue(producerSingleSystem.ShouldRunSystem());
            Assert.IsTrue(producerSingleSystem.Enabled);

            Assert.IsNotNull(producerParallelSystem);
            Assert.IsTrue(producerParallelSystem.ShouldRunSystem());
            Assert.IsTrue(producerParallelSystem.Enabled);

            Assert.IsNotNull(consumerSystem);
            Assert.IsTrue(consumerSystem.Enabled);

            var count = 0xFFFF / 2;

            producerSingleSystem.Count = producerParallelSystem.Count = count;

            World.Update();

            Assert.IsFalse(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, 0);

            producerSingleSystem.Count = producerParallelSystem.Count = 0;

            World.Update();

            Assert.IsTrue(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, count * 2);

            World.Update();

            Assert.IsFalse(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, 0);
        }

        [Test]
        public void TestZeroSizeTestDataProducerSinglePassWithoutAddJobHandleForProducerSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ZeroSizeTestDataProducerSingleSystem>();
            var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>());

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSystem);
            Assert.IsTrue(producerSystem.ShouldRunSystem());
            Assert.IsTrue(producerSystem.Enabled);

            producerSystem.AddJobHandleForProducer = false;
            producerSystem.Count = 0xFF;

            for (var index = 0; index < 60; index++) World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), producerSystem.Count);
        }

        [Test]
        public void TestValueTestDataProducerSingleSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerSingleSystem>();

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSystem);
            Assert.IsTrue(producerSystem.ShouldRunSystem());
            Assert.IsTrue(producerSystem.Enabled);

            var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ValueTestData>());
            var count = 0xFFFF;

            producerSystem.Count = count;

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), 0);

            producerSystem.Count = 0;

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), count);

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), 0);
        }

        [Test]
        public void TestValueTestDataProducerParallelSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerParallelSystem>();

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSystem);
            Assert.IsTrue(producerSystem.ShouldRunSystem());
            Assert.IsTrue(producerSystem.Enabled);

            var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ValueTestData>());
            var count = 0xFFFF;

            producerSystem.Count = count;

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), 0);

            producerSystem.Count = 0;

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), count);

            World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), 0);
        }

        [Test]
        public void TestValueTestDataConsumerSingleSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerSingleSystem>();
            var consumerSystem = World.GetExistingSystem<ValueTestDataConsumerSystem>();

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSystem);
            Assert.IsTrue(producerSystem.ShouldRunSystem());
            Assert.IsTrue(producerSystem.Enabled);

            Assert.IsNotNull(consumerSystem);
            Assert.IsTrue(consumerSystem.Enabled);

            var count = 0xFFFF;

            producerSystem.Count = count;

            World.Update();

            Assert.IsFalse(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, 0);

            producerSystem.Count = 0;

            World.Update();

            Assert.IsTrue(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, count);

            var testDataArray = consumerSystem.Query.ToComponentDataArray<ValueTestData>(Allocator.TempJob);

            testDataArray.Sort(default(ValueTestDataComparer));

            Assert.AreEqual(testDataArray.Length, count);

            for (var index = 0; index < count; index++) Assert.AreEqual(testDataArray[index].Value, index);

            testDataArray.Dispose();

            World.Update();

            Assert.IsFalse(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, 0);
        }

        [Test]
        public void TestValueTestDataConsumerParallelSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerParallelSystem>();
            var consumerSystem = World.GetExistingSystem<ValueTestDataConsumerSystem>();

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSystem);
            Assert.IsTrue(producerSystem.ShouldRunSystem());
            Assert.IsTrue(producerSystem.Enabled);

            Assert.IsNotNull(consumerSystem);
            Assert.IsTrue(consumerSystem.Enabled);

            var count = 0xFFFF;

            producerSystem.Count = count;

            World.Update();

            Assert.IsFalse(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, 0);

            producerSystem.Count = 0;

            World.Update();

            Assert.IsTrue(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, count);

            var testDataArray = consumerSystem.Query.ToComponentDataArray<ValueTestData>(Allocator.TempJob);

            testDataArray.Sort(default(ValueTestDataComparer));

            Assert.AreEqual(testDataArray.Length, count);

            for (var index = 0; index < count; index++) Assert.AreEqual(testDataArray[index].Value, index);

            testDataArray.Dispose();

            World.Update();

            Assert.IsFalse(consumerSystem.ShouldRunSystem());
            Assert.AreEqual(consumerSystem.Count, 0);
        }

        [Test]
        public void TestValueTestDataProducerSinglePassWithoutAddJobHandleForProducerSystem()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerSingleSystem>();
            var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ValueTestData>());

            Assert.IsNotNull(dispatcherSystem);
            Assert.IsTrue(dispatcherSystem.ShouldRunSystem());
            Assert.IsTrue(dispatcherSystem.Enabled);

            Assert.IsNotNull(producerSystem);
            Assert.IsTrue(producerSystem.ShouldRunSystem());
            Assert.IsTrue(producerSystem.Enabled);

            producerSystem.AddJobHandleForProducer = false;
            producerSystem.Count = 0xFF;

            for (var index = 0; index < 60; index++) World.Update();

            Assert.AreEqual(query.CalculateEntityCount(), producerSystem.Count);
        }

        [Test]
        public void TestZeroSizeTestDataProducerSingleSystemPerformance()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ZeroSizeTestDataProducerSingleSystem>();

            producerSystem.Count = 0xFFFF;

            Thread.SpinWait(60);

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            producerSystem.Update();
            stopwatch.Stop();

            var elapsedTimeProducer = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            dispatcherSystem.Update();
            stopwatch.Stop();

            var elapsedTimeDispatcher = stopwatch.Elapsed.TotalMilliseconds;
            var elapsedTimeTotal = elapsedTimeProducer + elapsedTimeDispatcher;
            var frameTime = TestsExtensions.GetFrameTimeSpanInMilliseconds(60);

            Debug.LogWarning(
                $"{nameof(TestZeroSizeTestDataProducerSingleSystemPerformance)} Count: {producerSystem.Count} Producer: {elapsedTimeProducer}ms Dispatcher: {elapsedTimeDispatcher}ms Total: {elapsedTimeTotal}ms out of {frameTime}ms ({elapsedTimeTotal / frameTime * 100}%)"
            );

            Assert.AreEqual(
                producerSystem.Count,
                EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>()).CalculateEntityCount()
            );
            Assert.IsTrue(elapsedTimeTotal < frameTime);
        }

        [Test]
        public void TestZeroSizeTestDataProducerSingleSystemMultipleQueuesPerformance()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ZeroSizeTestDataProducerSingleSystem>();
            var count = 0xFFFF;
            var iterations = 30;
            var countPerIteration = count / iterations;

            producerSystem.Count = countPerIteration;

            Thread.SpinWait(60);

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            for (var index = 0; index < iterations; index++) producerSystem.Update();

            stopwatch.Stop();

            var elapsedTimeProducer = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            dispatcherSystem.Update();
            stopwatch.Stop();

            var elapsedTimeDispatcher = stopwatch.Elapsed.TotalMilliseconds;
            var elapsedTimeTotal = elapsedTimeProducer + elapsedTimeDispatcher;
            var frameTime = TestsExtensions.GetFrameTimeSpanInMilliseconds(60);

            Debug.LogWarning(
                $"{nameof(TestZeroSizeTestDataProducerSingleSystemMultipleQueuesPerformance)} Count: {count} Queues: {iterations} ({countPerIteration} per queue) Producer: {elapsedTimeProducer}ms Dispatcher: {elapsedTimeDispatcher}ms Total: {elapsedTimeTotal}ms out of {frameTime}ms ({elapsedTimeTotal / frameTime * 100}%)"
            );

            Assert.AreEqual(
                countPerIteration * iterations,
                EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>()).CalculateEntityCount()
            );
            Assert.IsTrue(elapsedTimeTotal < frameTime);
        }

        [Test]
        public void TestZeroSizeTestDataProducerParallelSystemPerformance()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ZeroSizeTestDataProducerParallelSystem>();

            producerSystem.Count = 0xFFFF;

            Thread.SpinWait(60);

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            producerSystem.Update();
            stopwatch.Stop();

            var elapsedTimeProducer = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            dispatcherSystem.Update();
            stopwatch.Stop();

            var elapsedTimeDispatcher = stopwatch.Elapsed.TotalMilliseconds;
            var elapsedTimeTotal = elapsedTimeProducer + elapsedTimeDispatcher;
            var frameTime = TestsExtensions.GetFrameTimeSpanInMilliseconds(60);

            Debug.LogWarning(
                $"{nameof(TestZeroSizeTestDataProducerParallelSystemPerformance)} Count: {producerSystem.Count} Producer: {elapsedTimeProducer}ms Dispatcher: {elapsedTimeDispatcher}ms Total: {elapsedTimeTotal}ms out of {frameTime}ms ({elapsedTimeTotal / frameTime * 100}%)"
            );

            Assert.AreEqual(
                producerSystem.Count,
                EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>()).CalculateEntityCount()
            );
            Assert.IsTrue(elapsedTimeTotal < frameTime);
        }

        [Test]
        public void TestZeroSizeTestDataProducerParallelSystemMultipleQueuesPerformance()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ZeroSizeTestDataProducerParallelSystem>();
            var count = 0xFFFF;
            var iterations = 30;
            var countPerIteration = count / iterations;

            producerSystem.Count = countPerIteration;

            Thread.SpinWait(60);

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            for (var index = 0; index < iterations; index++) producerSystem.Update();

            stopwatch.Stop();

            var elapsedTimeProducer = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            dispatcherSystem.Update();
            stopwatch.Stop();

            var elapsedTimeDispatcher = stopwatch.Elapsed.TotalMilliseconds;
            var elapsedTimeTotal = elapsedTimeProducer + elapsedTimeDispatcher;
            var frameTime = TestsExtensions.GetFrameTimeSpanInMilliseconds(60);

            Debug.LogWarning(
                $"{nameof(TestZeroSizeTestDataProducerParallelSystemMultipleQueuesPerformance)} Count: {count} Queues: {iterations} ({countPerIteration} per queue) Producer: {elapsedTimeProducer}ms Dispatcher: {elapsedTimeDispatcher}ms Total: {elapsedTimeTotal}ms out of {frameTime}ms ({elapsedTimeTotal / frameTime * 100}%)"
            );

            Assert.AreEqual(
                countPerIteration * iterations,
                EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ZeroSizeTestData>()).CalculateEntityCount()
            );
            Assert.IsTrue(elapsedTimeTotal < frameTime);
        }

        [Test]
        public void TestValueTestDataProducerSingleSystemPerformance()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerSingleSystem>();

            producerSystem.Count = 0xFFFF;

            Thread.SpinWait(60);

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            producerSystem.Update();
            stopwatch.Stop();

            var elapsedTimeProducer = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            dispatcherSystem.Update();
            stopwatch.Stop();

            var elapsedTimeDispatcher = stopwatch.Elapsed.TotalMilliseconds;
            var elapsedTimeTotal = elapsedTimeProducer + elapsedTimeDispatcher;
            var frameTime = TestsExtensions.GetFrameTimeSpanInMilliseconds(60);

            Debug.LogWarning(
                $"{nameof(TestValueTestDataProducerSingleSystemPerformance)} Count: {producerSystem.Count} Producer: {elapsedTimeProducer}ms Dispatcher: {elapsedTimeDispatcher}ms Total: {elapsedTimeTotal}ms out of {frameTime}ms ({elapsedTimeTotal / frameTime * 100}%)"
            );

            Assert.AreEqual(
                producerSystem.Count,
                EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ValueTestData>()).CalculateEntityCount()
            );
            Assert.IsTrue(elapsedTimeTotal < frameTime);
        }

        [Test]
        public void TestValueTestDataProducerSingleSystemMultipleQueuesPerformance()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerSingleSystem>();
            var count = 0xFFFF;
            var iterations = 30;
            var countPerIteration = count / iterations;

            producerSystem.Count = countPerIteration;

            Thread.SpinWait(60);

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            for (var index = 0; index < iterations; index++) producerSystem.Update();

            stopwatch.Stop();

            var elapsedTimeProducer = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            dispatcherSystem.Update();
            stopwatch.Stop();

            var elapsedTimeDispatcher = stopwatch.Elapsed.TotalMilliseconds;
            var elapsedTimeTotal = elapsedTimeProducer + elapsedTimeDispatcher;
            var frameTime = TestsExtensions.GetFrameTimeSpanInMilliseconds(60);

            Debug.LogWarning(
                $"{nameof(TestValueTestDataProducerSingleSystemMultipleQueuesPerformance)} Count: {count} Queues: {iterations} ({countPerIteration} per queue) Producer: {elapsedTimeProducer}ms Dispatcher: {elapsedTimeDispatcher}ms Total: {elapsedTimeTotal}ms out of {frameTime}ms ({elapsedTimeTotal / frameTime * 100}%)"
            );

            Assert.AreEqual(
                countPerIteration * iterations,
                EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ValueTestData>()).CalculateEntityCount()
            );
            Assert.IsTrue(elapsedTimeTotal < frameTime);
        }

        [Test]
        public void TestValueTestDataProducerParallelSystemPerformance()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerParallelSystem>();

            producerSystem.Count = 0xFFFF;

            Thread.SpinWait(60);

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            producerSystem.Update();
            stopwatch.Stop();

            var elapsedTimeProducer = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            dispatcherSystem.Update();
            stopwatch.Stop();

            var elapsedTimeDispatcher = stopwatch.Elapsed.TotalMilliseconds;
            var elapsedTimeTotal = elapsedTimeProducer + elapsedTimeDispatcher;
            var frameTime = TestsExtensions.GetFrameTimeSpanInMilliseconds(60);

            Debug.LogWarning(
                $"{nameof(TestValueTestDataProducerParallelSystemPerformance)} Count: {producerSystem.Count} Producer: {elapsedTimeProducer}ms Dispatcher: {elapsedTimeDispatcher}ms Total: {elapsedTimeTotal}ms out of {frameTime}ms ({elapsedTimeTotal / frameTime * 100}%)"
            );

            Assert.AreEqual(
                producerSystem.Count,
                EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ValueTestData>()).CalculateEntityCount()
            );
            Assert.IsTrue(elapsedTimeTotal < frameTime);
        }

        [Test]
        public void TestValueTestDataProducerParallelSystemMultipleQueuesPerformance()
        {
            var dispatcherSystem = World.GetExistingSystem<SimulationDispatcherSystem>();
            var producerSystem = World.GetExistingSystem<ValueTestDataProducerParallelSystem>();
            var count = 0xFFFF;
            var iterations = 30;
            var countPerIteration = count / iterations;

            producerSystem.Count = countPerIteration;

            Thread.SpinWait(60);

            var stopwatch = new Stopwatch();

            stopwatch.Start();

            for (var index = 0; index < iterations; index++) producerSystem.Update();

            stopwatch.Stop();

            var elapsedTimeProducer = stopwatch.Elapsed.TotalMilliseconds;

            stopwatch.Restart();
            dispatcherSystem.Update();
            stopwatch.Stop();

            var elapsedTimeDispatcher = stopwatch.Elapsed.TotalMilliseconds;
            var elapsedTimeTotal = elapsedTimeProducer + elapsedTimeDispatcher;
            var frameTime = TestsExtensions.GetFrameTimeSpanInMilliseconds(60);

            Debug.LogWarning(
                $"{nameof(TestValueTestDataProducerParallelSystemMultipleQueuesPerformance)} Count: {count} Queues: {iterations} ({countPerIteration} per queue) Producer: {elapsedTimeProducer}ms Dispatcher: {elapsedTimeDispatcher}ms Total: {elapsedTimeTotal}ms out of {frameTime}ms ({elapsedTimeTotal / frameTime * 100}%)"
            );

            Assert.AreEqual(
                countPerIteration * iterations,
                EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ValueTestData>()).CalculateEntityCount()
            );
            Assert.IsTrue(elapsedTimeTotal < frameTime);
        }
    }
}