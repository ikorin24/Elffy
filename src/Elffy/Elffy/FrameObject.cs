using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;
using Elffy.Exceptions;

namespace Elffy
{
    /// <summary>
    /// ゲームによって管理され、ゲームのフレームに関わるオブジェクトの基底クラス<para/>
    /// ゲームのフレームに関する操作・ゲームによって管理されるための操作を提供します。<para/>
    /// </summary>
    public abstract class FrameObject
    {
        #region Proeprty
        /// <summary>このオブジェクトがゲームによって管理されているかどうかを返します</summary>
        public bool IsActivated { get; private set; }

        /// <summary>このオブジェクトが開始しているかどうかを返します</summary>
        internal bool IsStarted { get; set; }

        /// <summary>フレームのUpdate処理をスキップするかどうかを返します</summary>
        public bool IsFrozen { get; set; }

        /// <summary>このオブジェクトに付けられたタグ</summary>
        public string Tag { get; set; }

        /// <summary>このオブジェクトが、ゲームによって管理されるオブジェクトリストから破棄されているかどうかを返します</summary>
        public bool IsDestroyed { get; private set; }

        /// <summary>このオブジェクトが所属するレイヤー</summary>
        public LayerBase Layer { get; private set; }
        #endregion

        #region Method
        /// <summary>このオブジェクトが更新される最初のフレームに1度のみ実行される処理</summary>
        public virtual void Start() { }

        /// <summary>フレームの更新ごとに実行される更新処理</summary>
        public virtual void Update() { }

        /// <summary>このオブジェクトをゲーム管理下におきます</summary>
        public void Activate(LayerBase layer)
        {
            if(layer == null) { throw new ArgumentNullException(nameof(layer)); }
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
            if(IsActivated) { return; }
            Layer = layer;
            layer.AddFrameObject(this);
            IsActivated = true;
            OnActivated();
        }

        protected virtual void OnActivated() { }

        /// <summary>このオブジェクトをゲーム管理下から外して破棄します</summary>
        public virtual void Destroy()
        {
            if(IsDestroyed) { throw new ObjectDestroyedException(this); }
            Layer.RemoveFrameObject(this);
            Layer = null;
            IsDestroyed = true;
            (this as IDisposable)?.Dispose();
        }
        #endregion
    }
}
