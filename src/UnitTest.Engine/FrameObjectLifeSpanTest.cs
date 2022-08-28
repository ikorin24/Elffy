#nullable enable
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Shapes;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace UnitTest
{
    [Collection(TestEngineEntryPoint.UseEngineSymbol)]
    public sealed class FrameObjectLifeSpanTest
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
                Debug.Assert(screen is not null);
                await screen.Timings.Update.DelayFrame(4, ct);
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
                await screen!.Timings.Update.DelayFrame(4, ct);
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

        [Fact]
        public static void LifeSpan_FrameObject_InvalidOperation2() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // <New> -> call Activate() -> <Activating> -> call Terminate()
            // -> exception is catched -> <Alive> -> call Terminate() -> <Terminating> -> <Dead>
            // =================================================

            var cube = new Cube();
            cube.Activating.Subscribe(async (cube, ct) =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await cube.Terminate());
            });
            await cube.Activate(layer);
            Assert.Equal(LifeState.Alive, cube.LifeState);

            cube.Terminating.Subscribe((cube, ct) =>
            {
                Assert.Equal(LifeState.Terminating, cube.LifeState);
                return UniTask.CompletedTask;
            });
            await cube.Terminate();
            Assert.Equal(LifeState.Dead, cube.LifeState);
        });

        [Fact]
        public static void LifeSpan_FrameObject_OnThrown() => TestEngineEntryPoint.Start(UserCodeExceptionCatchMode.Throw, async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // <Activating> -> exception is thrown -> <Alive>
            // =================================================

            var cube = new Cube();
            cube.Activating.Subscribe((cube, ct) => throw new XunitException());
            await Assert.ThrowsAsync<XunitException>(async () => await cube.Activate(layer));
            Assert.Equal(LifeState.Alive, cube.LifeState);
        });

        [Fact]
        public static void LifeSpan_Positionable() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // cubes[0] -+-- cubes[1] ---- cubes[2]
            //           |-- cubes[3]
            //           `-- cubes[4] ---- cubes[5] ---- cubes[6]


            var cubes = await UniTask.WhenAll(Enumerable
                .Range(0, 7)
                .Select(i => new Cube().Activate(layer)));

            cubes[0].Children.Add(cubes[1]);
            cubes[0].Children.Add(cubes[3]);
            cubes[0].Children.Add(cubes[4]);
            cubes[1].Children.Add(cubes[2]);
            cubes[4].Children.Add(cubes[5]);
            cubes[5].Children.Add(cubes[6]);

            foreach(var cube in cubes) {
                Assert.Equal(LifeState.Alive, cube.LifeState);
            }

            // terminate 5 (and 6)
            await cubes[5].Terminate();
            Assert.Equal(LifeState.Dead, cubes[5].LifeState);
            Assert.Equal(LifeState.Dead, cubes[6].LifeState);
            Assert.Equal(0, cubes[4].Children.Count);


            Assert.Equal(3, cubes[0].Children.Count);


            // terminate 3
            await cubes[3].Terminate();
            Assert.Equal(LifeState.Dead, cubes[3].LifeState);


            // terminate 0 (and 1, 2, 4)
            await cubes[0].Terminate();
            Assert.Equal(0, cubes[0].Children.Count);
            foreach(var cube in cubes) {
                Assert.Equal(LifeState.Dead, cube.LifeState);
            }
        });

        [Fact]
        public static void LifeSpan_LayerDead() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // cubes[0] ---- cubes[1] ---- cubes[2] ---- cubes[3]

            var cubes = await UniTask.WhenAll(Enumerable
                .Range(0, 4)
                .Select(i => new Cube().Activate(layer)));

            cubes[0].Children.Add(cubes[1]);
            cubes[1].Children.Add(cubes[2]);
            cubes[2].Children.Add(cubes[3]);

            Assert.True(cubes[0].IsRoot);
            Assert.False(cubes[1].IsRoot);
            Assert.False(cubes[2].IsRoot);
            Assert.False(cubes[3].IsRoot);

            foreach(var cube in cubes) {
                Assert.Equal(layer, cube.Layer);
                Assert.Equal(LifeState.Alive, cube.LifeState);
            }

            await layer.Terminate();
            Assert.Equal(LifeState.Dead, layer.LifeState);

            foreach(var cube in cubes) {
                Assert.Null(cube.Layer);
                Assert.Equal(LifeState.Dead, cube.LifeState);
            }
        });

        [Fact]
        public static void Visibility_RenderableTree() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = await LayerPipelines
                .CreateBuilder(screen)
                .Build(() => new WorldLayer());

            // cubes[0] -+-- cubes[1] ---- cubes[2]
            //           |-- cubes[3]
            //           `-- cubes[4] ---- cubes[5] ---- cubes[6]


            var cubes = await UniTask.WhenAll(Enumerable
                .Range(0, 7)
                .Select(i => new Cube().Activate(layer)));

            cubes[0].Children.Add(cubes[1]);
            cubes[0].Children.Add(cubes[3]);
            cubes[0].Children.Add(cubes[4]);
            cubes[1].Children.Add(cubes[2]);
            cubes[4].Children.Add(cubes[5]);
            cubes[5].Children.Add(cubes[6]);

            foreach(var cube in cubes) {
                Assert.True(cube.IsVisible);
            }
            foreach(var cube in cubes) {
                Assert.Equal(RenderVisibility.Visible, cube.GetVisibility());
            }

            cubes[4].IsVisible = false;
            Assert.True(cubes[0].IsVisible);
            Assert.True(cubes[1].IsVisible);
            Assert.True(cubes[2].IsVisible);
            Assert.True(cubes[3].IsVisible);
            Assert.False(cubes[4].IsVisible);
            Assert.True(cubes[5].IsVisible);
            Assert.True(cubes[6].IsVisible);
            Assert.Equal(RenderVisibility.Visible, cubes[0].GetVisibility());
            Assert.Equal(RenderVisibility.Visible, cubes[1].GetVisibility());
            Assert.Equal(RenderVisibility.Visible, cubes[2].GetVisibility());
            Assert.Equal(RenderVisibility.Visible, cubes[3].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleSelf, cubes[4].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleHierarchical, cubes[5].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleHierarchical, cubes[6].GetVisibility());

            cubes[0].IsVisible = false;
            Assert.False(cubes[0].IsVisible);
            Assert.True(cubes[1].IsVisible);
            Assert.True(cubes[2].IsVisible);
            Assert.True(cubes[3].IsVisible);
            Assert.False(cubes[4].IsVisible);
            Assert.True(cubes[5].IsVisible);
            Assert.True(cubes[6].IsVisible);
            Assert.Equal(RenderVisibility.InvisibleSelf, cubes[0].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleHierarchical, cubes[1].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleHierarchical, cubes[2].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleHierarchical, cubes[3].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleSelf, cubes[4].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleHierarchical, cubes[5].GetVisibility());
            Assert.Equal(RenderVisibility.InvisibleHierarchical, cubes[6].GetVisibility());
        });
    }
}
