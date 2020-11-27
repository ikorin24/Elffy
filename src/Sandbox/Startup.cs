#nullable enable
using System.Diagnostics;
using Cysharp.Threading.Tasks;

namespace Sandbox
{
    public static class Startup
    {
        public static async void Start()
        {
            Definition.Initialize();

            // Load objects
            await UniTask.WhenAll(
                Definition.GenLight(),
                Definition.GenCameraMouse(),
                Definition.GenPlain(),
                Definition.GenAlicia(),
                Definition.GenFrog(),
                UniTask.WhenAll(
                    Definition.GenDice(),
                    Definition.GenDiceBehavior())
                .ContinueWith(x =>
                {
                    var (dice, behavior) = x;
                    behavior.Invoke(dice).Forget();
                }),
                Definition.GenBox(),
                Definition.GenBox2(),
                Definition.GenSky(),
                Definition.GenUI());

            Debug.WriteLine("Load completed !!");

            Definition.GenKeyBoardInputDump().Forget();
        }
    }
}
