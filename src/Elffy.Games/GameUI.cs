﻿#nullable enable
using Elffy.UI;
using System.ComponentModel;

namespace Elffy
{
    /// <summary>Provides game UI</summary>
    public static class GameUI
    {
        private static RootPanel? _rootPanel = null;

        public static RootPanel Root => _rootPanel!;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Initialize(RootPanel rootPanel)
        {
            _rootPanel = rootPanel;
        }
    }
}
