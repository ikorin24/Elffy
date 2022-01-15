#nullable enable
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Shapes;
using System;
using Xunit;

namespace UnitTest
{
    [Collection("UseEngine")]
    public sealed class LifeSpanTest
    {
        [Fact]
        public static void LifeSpan_FrameObject() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = new WorldLayer();
            await layer.Activate(screen);

            var cube = new Cube();
            Assert.Equal(LifeState.New, cube.LifeState);
            var isActivatingEventCalled = false;
            cube.Activating.Subscribe((cube, ct) =>
            {
                isActivatingEventCalled = true;
                Assert.Equal(LifeState.Activating, cube.LifeState);
                return UniTask.CompletedTask;
            });

            await cube.Activate(layer);
            Assert.True(isActivatingEventCalled);
            Assert.Equal(LifeState.Alive, cube.LifeState);

            await cube.Terminate();
            Assert.Equal(LifeState.Dead, cube.LifeState);
        });

        [Fact]
        public static void LifeSpan_FrameObject2() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // 1. <New> -> call Terminate() -> exception
            // 2. <Activating> -> call Activate() -> exception
            // =================================================

            var cube = new Cube();
            Assert.Equal(LifeState.New, cube.LifeState);

            // 1.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await cube.Terminate());
            Assert.Equal(LifeState.New, cube.LifeState);
            // ---------------------------------

            // 2.
            cube.Activating.Subscribe(async (cube, ct) =>
            {
                Assert.Equal(LifeState.Activating, cube.LifeState);
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await cube.Activate(layer));
            });
            await cube.Activate(layer);
            Assert.Equal(LifeState.Alive, cube.LifeState);
            // ---------------------------------

            await cube.Terminate();
            Assert.Equal(LifeState.Dead, cube.LifeState);
        });

        [Fact]
        public static void LifeSpan_Layer() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = new WorldLayer();
            Assert.Equal(LayerLifeState.New, layer.LifeState);
            await layer.Activate(screen);
            Assert.Equal(LayerLifeState.Alive, layer.LifeState);
            await layer.Terminate();
            Assert.Equal(LayerLifeState.Dead, layer.LifeState);
        });

        [Fact]
        public static void LifeSpan_Layer_FrameObject() => TestEngineEntryPoint.Start(async screen =>
        {
            // 1. Activate Layer
            // 2. Activate FrameObject
            // 3. Terminate FrameObject
            // 4. Terminate Layer
            // --------------------------------------------

            // 1.
            var layer = new WorldLayer();
            Assert.Equal(LayerLifeState.New, layer.LifeState);
            await layer.Activate(screen);
            Assert.Equal(LayerLifeState.Alive, layer.LifeState);

            // 2.
            var cube = new Cube();
            Assert.Equal(LifeState.New, cube.LifeState);
            var isActivatingEventCalled = false;
            cube.Activating.Subscribe((cube, ct) =>
            {
                isActivatingEventCalled = true;
                Assert.Equal(LifeState.Activating, cube.LifeState);
                return UniTask.CompletedTask;
            });
            await cube.Activate(layer);
            Assert.True(isActivatingEventCalled);
            Assert.Equal(LifeState.Alive, cube.LifeState);

            // 3.
            await cube.Terminate();
            Assert.Equal(LifeState.Dead, cube.LifeState);

            // 4.
            await layer.Terminate();
            Assert.Equal(LayerLifeState.Dead, layer.LifeState);
        });

        [Fact]
        public static void LifeSpan_Layer_FrameObject2() => TestEngineEntryPoint.Start(async screen =>
        {
            // 1. Activate Layer
            // 2. Activate FrameObject
            // 3. Terminate Layer
            // 4. No FrameObject are alive
            // --------------------------------------------

            // 1.
            var layer = new WorldLayer();
            Assert.Equal(LayerLifeState.New, layer.LifeState);
            await layer.Activate(screen);
            Assert.Equal(LayerLifeState.Alive, layer.LifeState);

            // 2.
            var cube = new Cube();
            Assert.Equal(LifeState.New, cube.LifeState);
            var isActivatingEventCalled = false;
            cube.Activating.Subscribe((cube, ct) =>
            {
                isActivatingEventCalled = true;
                Assert.Equal(LifeState.Activating, cube.LifeState);
                return UniTask.CompletedTask;
            });
            await cube.Activate(layer);
            Assert.True(isActivatingEventCalled);
            Assert.Equal(LifeState.Alive, cube.LifeState);

            // 3.
            await layer.Terminate();
            Assert.Equal(LayerLifeState.Dead, layer.LifeState);

            // 4.
            Assert.Equal(LifeState.Dead, cube.LifeState);
            Assert.Equal(0, layer.ObjectCount);
        });
    }
}
