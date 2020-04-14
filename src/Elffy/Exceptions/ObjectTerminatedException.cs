#nullable enable
using System;

namespace Elffy.Exceptions
{
    /// <summary>
    /// <see cref="FrameObject"/> が既に破棄された状態であることを示す例外
    /// </summary>
    public class ObjectTerminatedException : Exception
    {
        /// <summary>この例外の対象となっている <see cref="FrameObject"/></summary>
        public FrameObject TargetObject { get; }

        /// <summary><see cref="ObjectTerminatedException"/> コンストラクタ</summary>
        /// <param name="obj">この例外を発した <see cref="FrameObject"/></param>
        public ObjectTerminatedException(FrameObject obj) : base($"This object is already terminated. (Object type : '{obj.GetType().Name}')")
        {
            TargetObject = obj;
        }
    }
}
