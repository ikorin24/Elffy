using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Animation
{
    public class Animation
    {
        private Animation() { }
        private AnimationObject _animObj;
        private static readonly AnimationBehavior WAIT_BEHAVIOR = info => { };

        public static Animation Create() => new Animation();

        public Animation Begin(int time, AnimationBehavior behavior)
        {
            if(time <= 0) { throw new ArgumentException($"{nameof(time)} must be bigger than 1."); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }

            var animObj = GetAnimationObject();
            animObj.AddBehavior(time, behavior);
            return this;
        }

        public Animation Do(AnimationBehavior behavior)
        {
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }

            var animObj = GetAnimationObject();
            animObj.AddBehavior(behavior);
            return this;
        }

        public Animation Wait(int time)
        {
            if(time < 0) { throw new ArgumentException($"Time must be bigger than 0."); }

            var animObj = GetAnimationObject();
            animObj.AddBehavior(time, WAIT_BEHAVIOR);
            return this;
        }

        public Animation While(Func<bool> condition, AnimationBehavior behavior)
        {
            if(condition == null) { throw new ArgumentNullException(nameof(condition)); }
            if(behavior == null) { throw new ArgumentNullException(nameof(behavior)); }
            var animObj = GetAnimationObject();
            animObj.AddBehavior(condition, behavior);
            return this;
        }

        public void Cancel()
        {
            if(_animObj != null) {
                _animObj.IsCanceled = true;
            }
        }

        private AnimationObject GetAnimationObject()
        {
            if(_animObj == null) {
                _animObj = new AnimationObject();
                Game.AddGameObject(_animObj);
            }
            return _animObj;
        }
    }
}
