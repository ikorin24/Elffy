#nullable enable
using System;
using System.Linq;
using Elffy;
using Xunit;

namespace UnitTest
{
    public class PositionableTest
    {
        const int FloatPrecision = 5;

        [Fact]
        public void PositionableCollectionTest()
        {
            var root = new TestObject();
            Assert.True(root.Children.Count == 0);
            Assert.True(root.Children.AsSpan().IsEmpty);

            // Invalid instance throws exception
            Assert.Throws<InvalidOperationException>(() =>
            {
                var invalidCollection = new PositionableCollection();
                invalidCollection.Add(new TestObject());
            });

            // Add ten items
            var items = Enumerable.Range(0, 10).Select(i => new TestObject()).ToArray();
            foreach(var item in items) {
                root.Children.Add(item);
            }
            Assert.True(root.Children.Count == 10);

            // Check added items
            for(int i = 0; i < root.Children.Count; i++) {
                Assert.True(root.Children[i] == items[i]);
            }

            // Check iteration by GetEnumerator()
            {
                int i = 0;
                foreach(var item in root.Children) {
                    Assert.True(item == root.Children[i]);
                    i++;
                }
            }

            // Check iteration by AsSpan()
            {
                int i = 0;
                foreach(var item in root.Children.AsSpan()) {
                    Assert.True(item == root.Children[i]);
                    i++;
                }
            }

            // Check GetOffspring() method
            Assert.True(root.GetOffspring().SequenceEqual(items));
        }

        /// <summary>Positionable のツリー構造のワールド座標・ローカル座標が正しいかテストします</summary>
        [Fact]
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
            
            Assert.True(a.Parent == null);
            Assert.True(b.Parent == a);
            Assert.True(c.Parent == a);
            Assert.True(d.Parent == c);
            Assert.True(e.Parent == d);
            Assert.True(f.Parent == c);

            // 二重親不可
            Assert.Throws<InvalidOperationException>(() => a.Children.Add(f));


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

            AssertEqual(a.Position, pos1);
            AssertEqual(b.Position, Vector3.Zero);
            AssertEqual(c.Position, Vector3.Zero);     
            AssertEqual(d.Position, Vector3.Zero);             // a
            AssertEqual(e.Position, Vector3.Zero);             // |-- b
            AssertEqual(f.Position, Vector3.Zero);             // `-- c
            AssertEqual(a.WorldPosition, pos1);                //     |-- d
            AssertEqual(b.WorldPosition, pos1);                //     |   `- e
            AssertEqual(c.WorldPosition, pos1);                //     `-- f
            AssertEqual(d.WorldPosition, pos1);
            AssertEqual(e.WorldPosition, pos1);
            AssertEqual(f.WorldPosition, pos1);


            b.Position = pos2;
            c.Position = pos3;
            d.Position = pos4;
            e.Position = pos5;
            f.Position = pos6;

            AssertEqual(a.Position, pos1);
            AssertEqual(b.Position, pos2);
            AssertEqual(c.Position, pos3);
            AssertEqual(d.Position, pos4);                                     // a
            AssertEqual(e.Position, pos5);                                     // |-- b
            AssertEqual(f.Position, pos6);                                     // `-- c
            AssertEqual(a.WorldPosition, pos1);                                //     |-- d
            AssertEqual(b.WorldPosition, pos1 + pos2);                         //     |   `- e
            AssertEqual(c.WorldPosition, pos1 + pos3);                         //     `-- f
            AssertEqual(d.WorldPosition, pos1 + pos3 + pos4);
            AssertEqual(e.WorldPosition, pos1 + pos3 + pos4 + pos5);
            AssertEqual(f.WorldPosition, pos1 + pos3 + pos6);

            var diff = pos7 - c.WorldPosition;
            c.WorldPosition = pos7;

            AssertEqual(a.Position, pos1);                         // a
            AssertEqual(b.Position, pos2);                         // |-- b
            AssertEqual(c.Position, pos3 + diff);                  // `-- c
            AssertEqual(d.Position, pos4);                         //     |-- d
            AssertEqual(e.Position, pos5);                         //     |   `- e
            AssertEqual(f.Position, pos6);                         //     `-- f
            AssertEqual(a.WorldPosition, pos1);
            AssertEqual(b.WorldPosition, pos1 + pos2);
            AssertEqual(c.WorldPosition, pos7);
            AssertEqual(d.WorldPosition, pos7 + pos4);
            AssertEqual(e.WorldPosition, pos7 + pos4 + pos5);
            AssertEqual(f.WorldPosition, pos7 + pos6);


            // Get offspring, depth-first search test (If breadth-first search or other order, exception.)
            Assert.True(a.GetOffspring().SequenceEqual(new [] { b, c, d, e, f }));

            // Get Ancestor test
            Assert.True(e.GetAncestor().SequenceEqual(new[] { d, c, a }));
            Assert.True(f.GetAncestor().SequenceEqual(new[] { c, a }));

            Assert.Equal(a.GetRoot(), a);
            Assert.Equal(b.GetRoot(), a);
            Assert.Equal(c.GetRoot(), a);
            Assert.Equal(d.GetRoot(), a);
            Assert.Equal(e.GetRoot(), a);
            Assert.Equal(f.GetRoot(), a);
        }

        private class TestObject : Positionable
        {
        }

        private void AssertEqual(Vector3 expected, Vector3 actual)
        {
            Assert.Equal(expected.X, actual.X, FloatPrecision);
            Assert.Equal(expected.Y, actual.Y, FloatPrecision);
            Assert.Equal(expected.Z, actual.Z, FloatPrecision);
        }
    }
}
