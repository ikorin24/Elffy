#nullable enable
using Cysharp.Threading.Tasks;
using Elffy;
using Elffy.Shapes;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace UnitTest
{
    [Collection(TestEngineEntryPoint.UseEngineSymbol)]
    public sealed class LayerLifeSpanTest
    {
        [Fact]
        public static void LifeSpan_Layer() => TestEngineEntryPoint.Start(async screen =>
        {
            var layer = new ForwardRenderLayer();
            Assert.Equal(LifeState.New, layer.LifeState);

            var isSyncActivatingEventCalled = false;
            var isAsyncActivatingEventCalled = false;
            var isSyncTerminatingEventCalled = false;
            var isAsyncTerminatingEventCalled = false;

            layer.Activating.Subscribe((layer, ct) =>
            {
                Assert.False(isSyncActivatingEventCalled);
                isSyncActivatingEventCalled = true;
                Assert.Equal(LifeState.Activating, layer.LifeState);
                return UniTask.CompletedTask;
            });
            layer.Activating.Subscribe(async (layer, ct) =>
            {
                Assert.False(isAsyncActivatingEventCalled);
                Assert.Equal(LifeState.Activating, layer.LifeState);
                var screen = layer.Screen;
                Assert.NotNull(screen);
                Debug.Assert(screen is not null);
                await screen.Timings.Update.DelayFrame(4, ct);
                isAsyncActivatingEventCalled = true;
            });
            layer.Terminating.Subscribe((layer, ct) =>
            {
                Assert.False(isSyncTerminatingEventCalled);
                isSyncTerminatingEventCalled = true;
                Assert.Equal(LifeState.Terminating, layer.LifeState);
                return UniTask.CompletedTask;
            });
            layer.Terminating.Subscribe(async (layer, ct) =>
            {
                Assert.False(isAsyncTerminatingEventCalled);
                Assert.Equal(LifeState.Terminating, layer.LifeState);
                var screen = layer.Screen;
                Assert.NotNull(screen);
                Debug.Assert(screen is not null);
                await screen.Timings.Update.DelayFrame(4, ct);
                isAsyncTerminatingEventCalled = true;
            });

            await layer.Activate(screen);
            Assert.True(isSyncActivatingEventCalled);
            Assert.True(isAsyncActivatingEventCalled);
            Assert.Equal(LifeState.Alive, layer.LifeState);

            await layer.Terminate();
            Assert.True(isSyncTerminatingEventCalled);
            Assert.True(isAsyncTerminatingEventCalled);
            Assert.Equal(LifeState.Dead, layer.LifeState);
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
            var layer = await new ForwardRenderLayer().Activate(screen);
            Assert.Equal(0, layer.ObjectCount);

            // 2.
            var cubes = Enumerable.Range(0, 10).Select(i => new Cube()).ToArray();
            foreach(var cube in cubes) {
                Assert.Equal(LifeState.New, cube.LifeState);
                await cube.Activate(layer);
                Assert.Equal(LifeState.Alive, cube.LifeState);
            }
            Assert.True(cubes.All(cube => cube.LifeState == LifeState.Alive));
            Assert.Equal(cubes.Length, layer.ObjectCount);

            // 3.
            foreach(var cube in cubes) {
                Assert.Equal(LifeState.Alive, cube.LifeState);
                await cube.Terminate();
                Assert.Equal(LifeState.Dead, cube.LifeState);
            }
            Assert.True(cubes.All(cube => cube.LifeState == LifeState.Dead));
            Assert.Equal(0, layer.ObjectCount);

            // 4.
            await layer.Terminate();
            Assert.Equal(LifeState.Dead, layer.LifeState);
            Assert.Equal(0, layer.ObjectCount);
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
            var layer = await new ForwardRenderLayer().Activate(screen);

            // 2.
            var cubes = Enumerable.Range(0, 10).Select(i => new Cube()).ToArray();
            var terminatingCalled = new bool[cubes.Length];
            foreach(var (cube, i) in cubes.Select((x, i) => (x, i))) {
                Assert.Equal(LifeState.New, cube.LifeState);
                await cube.Activate(layer);
                Assert.Equal(LifeState.Alive, cube.LifeState);
                Assert.Equal(layer, cube.Layer);

                cube.Terminating.Subscribe((cube, ct) =>
                {
                    Assert.False(terminatingCalled[i]);
                    terminatingCalled[i] = true;
                    Assert.Equal(LifeState.Terminating, cube.LifeState);
                    Assert.Equal(layer, cube.Layer);
                    return UniTask.CompletedTask;
                });
            }
            Assert.True(cubes.All(cube => cube.LifeState == LifeState.Alive));

            // 3.
            await layer.Terminate();
            Assert.Equal(LifeState.Dead, layer.LifeState);

            // 4.
            Assert.True(cubes.All(cube => cube.LifeState == LifeState.Dead));
            Assert.True(terminatingCalled.All(x => x));
            Assert.Equal(0, layer.ObjectCount);
        });
    }
}
