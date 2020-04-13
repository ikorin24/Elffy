using System;
using System.Linq;
using Elffy.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//using OpenTK;
using Elffy;

namespace Test
{
    [TestClass]
    public class PositionableTest
    {
        /// <summary>Positionable のツリー構造のワールド座標・ローカル座標が正しいかテストします</summary>
        [TestMethod]
        public void PositionableTree()
        {
            // a
            // |-- b
            // `-- c
            //     |-- d
            //     |   `- e
            //     `-- f

            // ツリー構築
            var a = new TestObject();
            var b = new TestObject();
            var c = new TestObject();
            var d = new TestObject();
            var e = new TestObject();
            var f = new TestObject();
            a.Children.Add(b);
            a.Children.Add(c);
            c.Children.Add(d);
            c.Children.Add(f);
            d.Children.Add(e);
            TestHelper.Assert(a.Parent == null);
            TestHelper.Assert(b.Parent == a);
            TestHelper.Assert(c.Parent == a);
            TestHelper.Assert(d.Parent == c);
            TestHelper.Assert(e.Parent == d);
            TestHelper.Assert(f.Parent == c);

            // 二重親不可
            TestHelper.AssertException<InvalidOperationException>(() => a.Children.Add(f));


            var rand = new Random(12345);
            Vector3 GetRandomPos() => new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());

            var pos1 = GetRandomPos();
            var pos2 = GetRandomPos();
            var pos3 = GetRandomPos();
            var pos4 = GetRandomPos();
            var pos5 = GetRandomPos();
            var pos6 = GetRandomPos();

            var pos7 = GetRandomPos();

            a.Position = pos1;
            #region Check Value
            TestHelper.Assert(Equal(a.Position, pos1));
            TestHelper.Assert(Equal(b.Position, Vector3.Zero));
            TestHelper.Assert(Equal(c.Position, Vector3.Zero));     
            TestHelper.Assert(Equal(d.Position, Vector3.Zero));             // a
            TestHelper.Assert(Equal(e.Position, Vector3.Zero));             // |-- b
            TestHelper.Assert(Equal(f.Position, Vector3.Zero));             // `-- c
            TestHelper.Assert(Equal(a.WorldPosition, pos1));                //     |-- d
            TestHelper.Assert(Equal(b.WorldPosition, pos1));                //     |   `- e
            TestHelper.Assert(Equal(c.WorldPosition, pos1));                //     `-- f
            TestHelper.Assert(Equal(d.WorldPosition, pos1));
            TestHelper.Assert(Equal(e.WorldPosition, pos1));
            TestHelper.Assert(Equal(f.WorldPosition, pos1));
            #endregion

            b.Position = pos2;
            c.Position = pos3;
            d.Position = pos4;
            e.Position = pos5;
            f.Position = pos6;
            #region Check Value
            TestHelper.Assert(Equal(a.Position, pos1));
            TestHelper.Assert(Equal(b.Position, pos2));
            TestHelper.Assert(Equal(c.Position, pos3));
            TestHelper.Assert(Equal(d.Position, pos4));                                     // a
            TestHelper.Assert(Equal(e.Position, pos5));                                     // |-- b
            TestHelper.Assert(Equal(f.Position, pos6));                                     // `-- c
            TestHelper.Assert(Equal(a.WorldPosition, pos1));                                //     |-- d
            TestHelper.Assert(Equal(b.WorldPosition, pos1 + pos2));                         //     |   `- e
            TestHelper.Assert(Equal(c.WorldPosition, pos1 + pos3));                         //     `-- f
            TestHelper.Assert(Equal(d.WorldPosition, pos1 + pos3 + pos4));
            TestHelper.Assert(Equal(e.WorldPosition, pos1 + pos3 + pos4 + pos5));
            TestHelper.Assert(Equal(f.WorldPosition, pos1 + pos3 + pos6));
            #endregion

            var diff = pos7 - c.WorldPosition;
            c.WorldPosition = pos7;
            #region Check Value
            TestHelper.Assert(Equal(a.Position, pos1));                         // a
            TestHelper.Assert(Equal(b.Position, pos2));                         // |-- b
            TestHelper.Assert(Equal(c.Position, pos3 + diff));                  // `-- c
            TestHelper.Assert(Equal(d.Position, pos4));                         //     |-- d
            TestHelper.Assert(Equal(e.Position, pos5));                         //     |   `- e
            TestHelper.Assert(Equal(f.Position, pos6));                         //     `-- f
            TestHelper.Assert(Equal(a.WorldPosition, pos1));
            TestHelper.Assert(Equal(b.WorldPosition, pos1 + pos2));
            TestHelper.Assert(Equal(c.WorldPosition, pos7));
            TestHelper.Assert(Equal(d.WorldPosition, pos7 + pos4));
            TestHelper.Assert(Equal(e.WorldPosition, pos7 + pos4 + pos5));
            TestHelper.Assert(Equal(f.WorldPosition, pos7 + pos6));
            #endregion

            // Get offspring, depth-first search test (If breadth-first search or other order, exception.)
            Assert.IsTrue(a.GetOffspring().SequenceEqual(new [] { b, c, d, e, f }));

            // Get Ancestor test
            Assert.IsTrue(e.GetAncestor().SequenceEqual(new[] { d, c, a }));
            Assert.IsTrue(f.GetAncestor().SequenceEqual(new[] { c, a }));

            Assert.AreEqual(a.GetRoot(), a);
            Assert.AreEqual(b.GetRoot(), a);
            Assert.AreEqual(c.GetRoot(), a);
            Assert.AreEqual(d.GetRoot(), a);
            Assert.AreEqual(e.GetRoot(), a);
            Assert.AreEqual(f.GetRoot(), a);
        }

        private bool Equal(Vector3 v1, Vector3 v2)
        {
            float delta = 10e-4f;
            var diff = v1 - v2;
            return (Math.Abs(diff.X) < delta) && (Math.Abs(diff.Y) < delta) && (Math.Abs(diff.Z) < delta);
        }

        private bool Equal(float f1, float f2)
        {
            float delta = 10e-4f;
            return Math.Abs(f1 - f2) < delta;
        }

        private class TestObject : Positionable
        {
        }
    }
}
