using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;

namespace Elffy
{
    public abstract class GameObject : Positionable
    {
        internal bool IsStarted { get; set; }

        /// <summary>フレームのUpdate処理をスキップするかどうか</summary>
        public bool IsFrozen { get; set; }

        public string Tag { get; set; }

        protected bool IsDestroyed { get; private set; }

        public virtual void Start() { }

        public virtual void Update() { }

        public virtual void Destroy()
        {
            Game.RemoveGameObject(this);
            IsDestroyed = true;
        }
    }
}
