#nullable enable
using System;
using Elffy.Core;

namespace Elffy.Exceptions
{
    /// <summary>
    /// <see cref="ITerminatable"/> が既に破棄された状態であることを示す例外
    /// </summary>
    public class ObjectTerminatedException : Exception
    {
        /// <summary>この例外の対象となっている <see cref="ITerminatable"/></summary>
        public ITerminatable TargetObject { get; private set; }

        /// <summary><see cref="ObjectTerminatedException"/> コンストラクタ</summary>
        /// <param name="obj">この例外を発した <see cref="ITerminatable"/></param>
        public ObjectTerminatedException(ITerminatable obj) : base($"This object is already destroyed. (Object type : '{obj.GetType().Name}')")
        {
            TargetObject = obj;
        }
    }
}
