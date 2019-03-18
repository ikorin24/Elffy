using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public abstract class GameObject
    {
        internal bool IsStarted { get; set; }

        public string Tag { get; set; }

        protected bool IsDestroyed { get; private set; }

        public virtual void Render() { }

        public virtual void Start() { }

        public virtual void Update() { }

        public virtual void Destroy()
        {
            Game.RemoveGameObject(this);
            IsDestroyed = true;
        }
    }
}
