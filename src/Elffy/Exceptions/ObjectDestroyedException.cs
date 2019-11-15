#nullable enable
using System;
using Elffy.Core;

namespace Elffy.Exceptions
{
    /// <summary>
    /// <see cref="IDestroyable"/> が既に破棄された状態であることを示す例外
    /// </summary>
    public class ObjectDestroyedException : Exception
    {
        /// <summary>この例外の対象となっている <see cref="IDestroyable"/></summary>
        public IDestroyable TargetObject { get; private set; }

        /// <summary><see cref="ObjectDestroyedException"/> コンストラクタ</summary>
        /// <param name="obj">この例外を発した <see cref="IDestroyable"/></param>
        public ObjectDestroyedException(IDestroyable obj) : base($"This object is already destroyed. (Object type : '{obj.GetType().Name}')")
        {
            TargetObject = obj;
        }
    }
}
