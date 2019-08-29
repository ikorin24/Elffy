using Elffy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Physics
{
    public abstract class Rigidbody : Positionable
    {
        internal RigidbodyType Type { get; private set; }

        /// <summary>衝突時イベント</summary>
        public event CollisionEventHandler Collided;

        internal Rigidbody(RigidbodyType type)
        {
            Type = type;
        }

        /// <summary>衝突判定を行います</summary>
        /// <remarks>高速化のため引数の null チェックは省略して実装してください</remarks>
        /// <param name="target">衝突判定を行う対象の <see cref="Rigidbody"/></param>
        /// <returns>衝突しているか</returns>
        internal abstract bool CollideWith(Rigidbody target);
    }

    /// <summary>衝突イベントハンドラ</summary>
    /// <param name="sender">sender of the event</param>
    /// <param name="e">衝突イベントのイベント引数</param>
    public delegate void CollisionEventHandler(object sender, CollisiontEventArgs e);

    #region class CollisiontEventArgs
    /// <summary>衝突イベントのイベント引数クラス</summary>
    public class CollisiontEventArgs : EventArgs
    {
        /// <summary>衝突された <see cref="Rigidbody"/></summary>
        public Rigidbody Self { get; }
        /// <summary>衝突した <see cref="Rigidbody"/></summary>
        public Rigidbody Target { get; }

        /// <summary>コンストラクタ</summary>
        /// <param name="self">衝突された <see cref="Rigidbody"/></param>
        /// <param name="target">衝突した <see cref="Rigidbody"/></param>
        internal CollisiontEventArgs(Rigidbody self, Rigidbody target)
        {
            Self = self ?? throw new ArgumentNullException(nameof(self));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }
    }
    #endregion

    public enum RigidbodyType
    {
        Rigidbody3D,
        Rigidbody2D,
    }
}
