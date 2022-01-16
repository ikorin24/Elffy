#nullable enable
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Shapes;
using System;
using Xunit;
using Xunit.Sdk;

namespace UnitTest
{
    [Collection("UseEngine")]
    public sealed class LifeSpanTest
    {
        [Fact]
        public static void LifeSpan_FrameObject() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // <New> -> call Activate() -> <Activating> -> [next frame] -> <Alive>
            // -> call Terminate() -> <Terminating> -> [next frame] -> <Dead>

            var cube = new Cube();
            Assert.Equal(LifeState.New, cube.LifeState);
            var isSyncActivatingEventCalled = false;
            var isAsyncActivatingEventCalled = false;
            var isSyncTerminatingEventCalled = false;
            var isAsyncTerminatingEventCalled = false;
            cube.Activating.Subscribe((cube, ct) =>
            {
                Assert.False(isSyncActivatingEventCalled);
                isSyncActivatingEventCalled = true;
                Assert.Equal(LifeState.Activating, cube.LifeState);
                return UniTask.CompletedTask;
            });
            cube.Activating.Subscribe(async (cube, ct) =>
            {
                Assert.False(isAsyncActivatingEventCalled);
                Assert.Equal(LifeState.Activating, cube.LifeState);
                var screen = cube.Screen;
                Assert.NotNull(screen);
                await screen!.TimingPoints.Update.DelayFrame(4, ct);
                isAsyncActivatingEventCalled = true;
                Assert.Equal(LifeState.Activating, cube.LifeState);
            });
            cube.Terminating.Subscribe((cube, ct) =>
            {
                Assert.False(isSyncTerminatingEventCalled);
                isSyncTerminatingEventCalled = true;
                Assert.Equal(LifeState.Terminating, cube.LifeState);
                return UniTask.CompletedTask;
            });
            cube.Terminating.Subscribe(async (cube, ct) =>
            {
                Assert.False(isAsyncTerminatingEventCalled);
                Assert.Equal(LifeState.Terminating, cube.LifeState);
                var screen = cube.Screen;
                Assert.NotNull(screen);
                await screen!.TimingPoints.Update.DelayFrame(4, ct);
                isAsyncTerminatingEventCalled = true;
                Assert.Equal(LifeState.Terminating, cube.LifeState);
            });

            await cube.Activate(layer);
            Assert.True(isSyncActivatingEventCalled);
            Assert.True(isAsyncActivatingEventCalled);
            Assert.Equal(LifeState.Alive, cube.LifeState);

            await cube.Terminate();
            Assert.True(isSyncTerminatingEventCalled);
            Assert.True(isAsyncTerminatingEventCalled);
            Assert.Equal(LifeState.Dead, cube.LifeState);
        });

        [Fact]
        public static void LifeSpan_FrameObject_InvalidOperation() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // 1. <New> -> call Terminate() -> exception is catched -> <New>
            // 2. <Activating> -> call Activate() -> exception is catched -> <Alive>
            // 3. <Alive> -> call Activate() -> exception is catched -> <Alive>
            // 4. <Alive> -> call Terminate() -> <Terminating> -> call Activate()
            //    -> exception is catched -> <Terminating> -> [next frame] -> <Dead>
            // =================================================

            // 1.
            var cube = new Cube();
            Assert.Equal(LifeState.New, cube.LifeState);
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

            // 3.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await cube.Activate(layer));
            Assert.Equal(LifeState.Alive, cube.LifeState);

            // 4.
            cube.Terminating.Subscribe(async (cube, ct) =>
            {
                Assert.Equal(LifeState.Terminating, cube.LifeState);
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await cube.Activate(layer));
                Assert.Equal(LifeState.Terminating, cube.LifeState);
            });
            await cube.Terminate();
            Assert.Equal(LifeState.Dead, cube.LifeState);
        });

        [Fact(Skip = "Not implemented yet")]
        public static void LifeSpan_FrameObject_ValidOperation() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // <New> -> call Activate() -> <Activating> -> call Terminate() -> <Dead>
            // =================================================

            var cube = new Cube();
            cube.Activating.Subscribe(async (cube, ct) =>
            {
                await cube.Terminate();
            });
            await cube.Activate(layer);
            Assert.Equal(LifeState.Dead, cube.LifeState);
        });



        [Fact]
        public static void LifeSpan_FrameObject_OnThrown() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // <Activating> -> exception is thrown -> <Dead>
            // =================================================

            var cube = new Cube();
            cube.Activating.Subscribe((cube, ct) => throw new XunitException());
            await Assert.ThrowsAsync<XunitException>(async () => await cube.Activate(layer));
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
