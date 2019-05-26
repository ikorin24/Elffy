using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;

namespace Elffy
{
    public abstract class FrameObject
    {
        internal bool IsStarted { get; set; }

        /// <summary>フレームのUpdate処理をスキップするかどうか</summary>
        public bool IsFrozen { get; set; }

        public string Tag { get; set; }

        protected bool IsDestroyed { get; private set; }

        public virtual void Start() { }

        public virtual void Update() { }

        public void Activate()
        {
            Game.AddFrameObject(this);
        }

        public virtual void Destroy()
        {
            Game.RemoveFrameObject(this);
            IsDestroyed = true;
            (this as IDisposable)?.Dispose();
        }
    }
}
