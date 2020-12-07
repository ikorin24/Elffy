#nullable enable
using Elffy.UI;

namespace Elffy
{
    public static class GameUI
    {
        private static RootPanel? _rootPanel = null;

        public static RootPanel Root => _rootPanel!;

        internal static void Initialize(RootPanel rootPanel)
        {
            _rootPanel = rootPanel;
        }
    }
}
