using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Exceptions
{
    /// <summary>
    /// <see cref="FrameObject"/> が既に破棄された状態であることを示す例外
    /// </summary>
    public class ObjectDestroyedException : Exception
    {
        /// <summary>この例外の対象となっている <see cref="FrameObject"/></summary>
        public FrameObject TargetObject { get; private set; }

        /// <summary><see cref="ObjectDestroyedException"/> コンストラクタ</summary>
        /// <param name="obj">この例外を発した <see cref="FrameObject"/></param>
        public ObjectDestroyedException(FrameObject obj) : base($"This object is already destroyed. (Object type : '{obj?.GetType().Name}')")
        {
            TargetObject = obj;
        }
    }
}
