#nullable enable

namespace Elffy.Core
{
    internal enum FrameObjectLifeState : byte
    {
        /// <summary><see cref="FrameObject"/> の初期状態。Activate されていない状態</summary>
        New,
        /// <summary><see cref="New"/> の状態から Activate が呼ばれ、Activate キュー内にある状態</summary>
        Activated,
        /// <summary><see cref="Activated"/> の後に Activate キューが処理され、Update 等が呼ばれる状態</summary>
        Alive,
        /// <summary><see cref="Alive"/> の状態から Terminate が呼ばれ、Terminate キュー内にある状態</summary>
        Terminated,
        /// <summary><see cref="Terminated"/> の後に Terminate キューが処理され、管理下から外れた状態</summary>
        Dead,
    }
}
