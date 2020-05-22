#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy;

namespace Elffy.Core
{
    internal static class FrameObjectPool<T> where T : FrameObject
    {
        internal static void Pool(T obj)
        {
            throw new NotImplementedException();
        }

        internal static bool TryGet(out T obj)
        {
            throw new NotImplementedException();
        }
    }
}
