#nullable enable
using Cysharp.Threading.Tasks;

namespace Sandbox
{
    public static class Startup
    {
        public static async void Start()
        {
            Definition.Initialize();

            await Definition.GenUI();

            var dice = await Definition.GenDice();
            var behavior = await Definition.GenDiceBehavior();
            behavior(dice).Forget();

            Definition.GenKeyBoardInputDump().Forget();

            await UniTask.WhenAll(
                //Definition.GenLight(),
                Definition.GenCameraMouse(),
                Definition.GenPlain(),
                Definition.GenAlicia(),
                Definition.GenFrog(),
                Definition.GenBox2(),
                Definition.GenSky());
        }
    }
}
