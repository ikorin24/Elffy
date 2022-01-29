#nullable enable
using System.Linq;
using Xunit;
using Elffy;
using Elffy.UI;
using Cysharp.Threading.Tasks;

namespace UnitTest
{
    [Collection(TestEngineEntryPoint.UseEngineSymbol)]
    public sealed class ControlLifeSpanTest
    {
        [Fact]
        public static void LifeSpan_Control() => TestEngineEntryPoint.Start(async screen =>
        {
            var uiLayer = await new UILayer().Activate(screen);
            var uiRoot = uiLayer.UIRoot;
            Assert.Equal(0, uiRoot.Children.Count);

            var button = new Button();
            Assert.Equal(LifeState.New, button.LifeState);

            await uiRoot.Children.Add(button);
            Assert.Equal(LifeState.Alive, button.LifeState);
            Assert.Equal(1, uiRoot.Children.Count);

            await uiRoot.Children.Remove(button);
            Assert.Equal(LifeState.Dead, button.LifeState);
            Assert.Equal(0, uiRoot.Children.Count);
        });

        [Fact]
        public static void LifeSpan_Control2() => TestEngineEntryPoint.Start(async screen =>
        {
            var uiLayer = await new UILayer().Activate(screen);
            var uiRoot = uiLayer.UIRoot;
            Assert.Equal(0, uiRoot.Children.Count);

            var buttons = Enumerable.Range(0, 10).Select(i => new Button()).ToArray();

            await UniTask.WhenAll(buttons.Select(async (button, i) =>
            {
                Assert.Equal(LifeState.New, button.LifeState);
                await uiRoot.Children.Add(button);
                Assert.Equal(LifeState.Alive, button.LifeState);
            }).ToArray());
            Assert.True(buttons.All(b => b.LifeState == LifeState.Alive));
            Assert.Equal(buttons.Length, uiRoot.Children.Count);

            await UniTask.WhenAll(buttons.Select(async (button, i) =>
            {
                Assert.Equal(LifeState.Alive, button.LifeState);
                await uiRoot.Children.Remove(button);
                Assert.Equal(LifeState.Dead, button.LifeState);
            }).ToArray());
            Assert.True(buttons.All(b => b.LifeState == LifeState.Dead));
            Assert.Equal(0, uiRoot.Children.Count);
        });

        [Fact]
        public static void LifeSpan_Control_Clear() => TestEngineEntryPoint.Start(async screen =>
        {
            var uiLayer = await new UILayer().Activate(screen);
            var uiRoot = uiLayer.UIRoot;
            Assert.Equal(0, uiRoot.Children.Count);

            var buttons = Enumerable.Range(0, 10).Select(i => new Button()).ToArray();
            foreach(var button in buttons) {
                await uiRoot.Children.Add(button);
            }
            Assert.True(buttons.All(b => b.LifeState == LifeState.Alive));
            Assert.Equal(buttons.Length, uiRoot.Children.Count);

            await uiRoot.Children.Clear();
            Assert.Equal(0, uiRoot.Children.Count);
            Assert.True(buttons.All(b => b.LifeState == LifeState.Dead));
        });

        [Fact]
        public static void LifeSpan_Control_Tree() => TestEngineEntryPoint.Start(async screen =>
        {
            var uiLayer = await new UILayer().Activate(screen);
            var uiRoot = uiLayer.UIRoot;
            Assert.Equal(0, uiRoot.Children.Count);

            var buttons = Enumerable.Range(0, 10).Select(i => new Button()).ToArray();

            // <root> --+-- [0]
            //          |-- [1] --+-- [2]
            //          |         |-- [3] ----- [4]
            //          |         `-- [5]
            //          `-- [6] ----- [7] ----- [8] ----- [9]

            await uiRoot.Children.Add(buttons[0]);
            await uiRoot.Children.Add(buttons[1]);
            {
                await buttons[1].Children.Add(buttons[2]);
                await buttons[1].Children.Add(buttons[3]);
                {
                    await buttons[3].Children.Add(buttons[4]);
                }
                await buttons[1].Children.Add(buttons[5]);
            }
            await uiRoot.Children.Add(buttons[6]);
            {
                await buttons[6].Children.Add(buttons[7]);
                {
                    await buttons[7].Children.Add(buttons[8]);
                    {
                        await buttons[8].Children.Add(buttons[9]);
                    }
                }
            }

            Assert.True(buttons.All(b => b.LifeState == LifeState.Alive));
            Assert.True(buttons.All(b => b.Parent is not null));

            #region Assert tree
            Assert.Equal(3, uiRoot.Children.Count);
            Assert.True(uiRoot.Children.ToArray().SequenceEqual(new[] {
                buttons[0],
                buttons[1],
                buttons[6],
            }));

            Assert.Equal(0, buttons[0].Children.Count);
            Assert.True(buttons[0].Children.ToArray().SequenceEqual(new Control[0]));

            Assert.Equal(3, buttons[1].Children.Count);
            Assert.True(buttons[1].Children.ToArray().SequenceEqual(new[] {
                buttons[2],
                buttons[3],
                buttons[5],
            }));

            Assert.Equal(0, buttons[2].Children.Count);
            Assert.True(buttons[2].Children.ToArray().SequenceEqual(new Control[0]));

            Assert.Equal(1, buttons[3].Children.Count);
            Assert.True(buttons[3].Children.ToArray().SequenceEqual(new[] {
                buttons[4],
            }));

            Assert.Equal(0, buttons[4].Children.Count);
            Assert.True(buttons[4].Children.ToArray().SequenceEqual(new Control[0]));

            Assert.Equal(0, buttons[5].Children.Count);
            Assert.True(buttons[5].Children.ToArray().SequenceEqual(new Control[0]));

            Assert.Equal(1, buttons[6].Children.Count);
            Assert.True(buttons[6].Children.ToArray().SequenceEqual(new[] {
                buttons[7],
            }));

            Assert.Equal(1, buttons[7].Children.Count);
            Assert.True(buttons[7].Children.ToArray().SequenceEqual(new[] {
                buttons[8],
            }));

            Assert.Equal(1, buttons[8].Children.Count);
            Assert.True(buttons[8].Children.ToArray().SequenceEqual(new[] {
                buttons[9],
            }));

            Assert.Equal(0, buttons[9].Children.Count);
            Assert.True(buttons[9].Children.ToArray().SequenceEqual(new Control[0]));
            #endregion

            await buttons[3].Children.Remove(buttons[4]);
            Assert.Equal(LifeState.Alive, buttons[3].LifeState);
            Assert.Equal(0, buttons[3].Children.Count);
            Assert.Equal(LifeState.Dead, buttons[4].LifeState);
            Assert.True(buttons[4].Parent is null);


            await buttons[6].Children.Remove(buttons[7]);
            Assert.Equal(LifeState.Alive, buttons[6].LifeState);
            Assert.Equal(0, buttons[6].Children.Count);
            Assert.Equal(0, buttons[7].Children.Count);
            Assert.Equal(0, buttons[8].Children.Count);
            Assert.Equal(LifeState.Alive, buttons[6].LifeState);
            Assert.Equal(LifeState.Dead, buttons[7].LifeState);
            Assert.Equal(LifeState.Dead, buttons[8].LifeState);

            await uiRoot.Children.Clear();
            Assert.Equal(0, uiRoot.Children.Count);
            Assert.Equal(LifeState.Alive, uiRoot.LifeState);
            Assert.True(buttons.All(b => b.Parent is null));
            Assert.True(buttons.All(b => b.LifeState == LifeState.Dead));
            Assert.True(buttons.All(b => b.Children.Count == 0));
        });
    }
}
