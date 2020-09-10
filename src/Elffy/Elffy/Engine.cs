#nullable enable
using System;
using Elffy.UI;
using System.Drawing;
using Elffy.Threading;
using Elffy.Platforms;
using Elffy.Exceptions;
using Elffy.Platforms.Windows;
using Elffy.Effective.Unsafes;
using Elffy.Core;
using System.Runtime.CompilerServices;
using Elffy.OpenGL;
using Elffy.AssemblyServices;
using System.Collections.Generic;

namespace Elffy
{
    public static class Engine
    {
        private static DefaultGLResource? _glResource;

        private static SyncContextReceiver _syncContextReciever = new SyncContextReceiver();
        private static LazyApplyingList<IHostScreen> _screens = new LazyApplyingList<IHostScreen>();


        public static TextureObject WhiteEmptyTexture
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _glResource?.WhiteEmptyTexture
                ?? throw new InvalidOperationException("Engine is already runnning.");
        }

        public static bool IsRunning { get; private set; }

        public static void Run()
        {
            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            IsRunning = true;
            Dispatcher.SetMainThreadID();
        }

        public static void Start(IHostScreen screen, bool show = true)
        {
            if(screen is null) { throw new ArgumentNullException(nameof(screen)); }

            if(IsRunning) { throw new InvalidOperationException("Engine is already runnning."); }
            IsRunning = true;

            try {
                Dispatcher.SetMainThreadID();
                Resources.Initialize();
                CustomSynchronizationContext.Create(_syncContextReciever);
                _glResource = new DefaultGLResource();
                _glResource.Create();
                //AddScreen(screen, show);
                if(show) {
                    screen.Show();
                }

                while(HandleOnce()) ;
            }
            finally {
                _glResource?.Dispose();
                _glResource = null;
                CustomSynchronizationContext.Delete();
                if(AssemblyState.IsDebug) {
                    // TODO: 暫定的実装
                    GC.Collect();           // OpenGL 関連のメモリリーク検知用
                }
            }
        }

        internal static void AddScreen(IHostScreen screen, bool show = true)
        {
            _screens.Add(screen);
            if(show) {
                screen.Show();
            }
        }

        internal static void RemoveScreen(IHostScreen screen)
        {
            _screens.Remove(screen);
            screen.Dispose();
        }

        public static bool HandleOnce()
        {
            _syncContextReciever.DoAll();
            _screens.ApplyAdd();
            foreach(var s in _screens.AsSpan()) {
                s.HandleOnce();
            }
            _screens.ApplyRemove();
            return _screens.Count != 0;
        }

        public static void End()
        {
            if(!IsRunning) { return; }
            IsRunning = false;
            if(_glResource is null == false) {
                _glResource.Dispose();
                _glResource = null;
            }
            if(AssemblyState.IsDebug) {
                // TODO: 暫定的実装
                GC.Collect();           // OpenGL 関連のメモリリーク検知用
            }
        }

        //public static void ShowScreen(ActionEventHandler<IHostScreen> initialized)
        //    => ShowScreen(800, 450, "", initialized);

        //public static void ShowScreen(int width, int height, string title, ActionEventHandler<IHostScreen> initialized)
        //    => ShowScreen(width, height, title, null, WindowStyle.Default, initialized);

        //public static void ShowScreen(int width, int height, string title, Icon? icon, WindowStyle windowStyle,
        //                              ActionEventHandler<IHostScreen> initialized)
        //{
        //    if(initialized is null) { throw new ArgumentNullException(nameof(initialized)); }
        //    if(!IsRunning) { throw new InvalidOperationException($"{nameof(Engine)} is not running."); }
        //    try {
        //        IHostScreen screen;
        //        switch(Platform.PlatformType) {
        //            case PlatformType.Windows:
        //            case PlatformType.MacOSX:
        //            case PlatformType.Unix: {
        //                screen = new Window(width, height, title, windowStyle);
        //                break;
        //            }
        //            case PlatformType.Android:
        //            case PlatformType.Other:
        //            default:
        //                throw Platform.PlatformNotSupported();
        //        }
        //        var glResource = new DefaultGLResource();
        //        glResource.Create();
        //        _glResource = glResource;
        //        screen.Initialized += initialized;
        //        screen.Show(width, height, title, icon, windowStyle);
        //    }
        //    finally {
        //        CustomSynchronizationContext.Delete();
        //    }
        //}
    }


    internal sealed class LazyApplyingList<T>
    {
        private readonly List<T> _list;
        private readonly List<T> _addedList;
        private readonly List<T> _removedList;

        public int Count => _list.Count;

        public LazyApplyingList()
        {
            _list = new List<T>();
            _addedList = new List<T>();
            _removedList = new List<T>();
        }

        public void Add(in T item)
        {
            _addedList.Add(item);
        }

        public void Remove(in T item)
        {
            _removedList.Add(item);
        }

        public void ApplyAdd()
        {
            if(_addedList.Count == 0) { return; }
            Apply();

            void Apply()
            {
                _list.AddRange(_addedList);
                _addedList.Clear();
            }
        }

        public void ApplyRemove()
        {
            if(_removedList.Count == 0) { return; }
            Apply();

            void Apply()
            {
                foreach(var item in _removedList.AsSpan()) {
                    _list.Remove(item);
                }
                _removedList.Clear();
            }
        }

        public Span<T> AsSpan() => _list.AsSpan();
    }
}
