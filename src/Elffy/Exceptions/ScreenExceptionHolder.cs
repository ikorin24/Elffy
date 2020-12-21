#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Elffy.Exceptions
{
    //internal static class ScreenExceptionHolder
    //{
    //    private static readonly ConditionalWeakTable<IHostScreen, List<Exception>> _exceptions = new();
    //    private static readonly object _lockObj = new();

    //    public static void SetException(IHostScreen? screen, Exception ex)
    //    {
    //        if(screen is null) { return; }
    //        lock(_lockObj) {
    //            _exceptions.GetValue(screen, _ => new()).Add(ex);
    //        }
    //    }

    //    public static void ThrowIfExceptionExists(IHostScreen? screen)
    //    {
    //        if(screen is null) { return; }
    //        var exist = false;
    //        List<Exception>? list = null;
    //        lock(_lockObj) {
    //            exist = _exceptions.TryGetValue(screen, out list);
    //            _exceptions.Remove(screen);
    //        }
    //        if(exist) {
    //            if(list!.Count == 1) {
    //                ExceptionDispatchInfo.Throw(list[0]);
    //            }
    //            else if(list.Count > 1) {
    //                ExceptionDispatchInfo.Throw(new AggregateException(list));
    //            }
    //        }
    //    }
    //}
}
