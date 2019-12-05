using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Elffy.Effective;
using System.Linq;
using static Test.TestHelper;
using System.Runtime.InteropServices;

namespace Test
{
    [TestClass]
    public class UnmanagedArrayTest
    {
        [TestMethod]
        public void ReadWrite()
        {
            var len = 100;

            var array = new UnmanagedArray<int>(len);
            for(int i = 0; i < array.Length; i++) {
                array[i] = i * i;
            }
            for(int i = 0; i < len; i++) {
                if(array[i] != i * i) { throw new Exception(); }
            }
            array.Free();
        }

        [TestMethod]
        public void BasicAPI()
        {
            // UnmanagedArrayの提供するpublicなAPIを網羅的にテストします

            // プロパティ
            using(var array = new UnmanagedArray<short>(10000)) {
                Assert(array.IsReadOnly == false);
                AssertException<IndexOutOfRangeException>(() => array[-1] = 4);
                AssertException<IndexOutOfRangeException, int>(() => array[-8]);
                AssertException<IndexOutOfRangeException>(() => array[array.Length] = 4);
                AssertException<IndexOutOfRangeException, int>(() => array[array.Length]);
            }

            // 要素数
            for(int i = 0; i < 10; i++) {
                using(var array = new UnmanagedArray<double>(i)) {
                    Assert(array.Length == i);
                }
            }

            // 手動解放/二重解放防止
            {
                var array = new UnmanagedArray<float>(10);
                array.Free();
                AssertException<InvalidOperationException>(() => array[0] = 3);
                AssertException<InvalidOperationException, float>(() => array[7]);
                array.Free();
            }

            // 配列との相互変換
            {
                var rand = new Random(12345678);
                var origin = Enumerable.Range(0, 100).Select(i => rand.Next()).ToArray();
                using(var array = origin.ToUnmanagedArray()) {
                    for(int i = 0; i < array.Length; i++) {
                        Assert(array[i] == origin[i]);
                    }
                    var copy = origin.ToArray();
                    for(int i = 0; i < copy.Length; i++) {
                        Assert(array[i] == origin[i]);
                    }
                }
            }

            // 列挙/LINQ
            using(var array = new UnmanagedArray<bool>(100)) {
                foreach(var item in array) {
                    Assert(item == false);
                }
                for(int i = 0; i < array.Length; i++) {
                    array[i] = true;
                }
                Assert(array.All(x => x));
                var rand1 = new Random(1234);
                var rand2 = new Random(1234);
                var seq1 = array.Select(x => rand1.Next());
                var seq2 = Enumerable.Range(0, array.Length).Select(x => rand2.Next());
                Assert(seq1.SequenceEqual(seq2));
            }

            // その他メソッド
            AssertException<ArgumentException>(() => new UnmanagedArray<ulong>(-4));
            using(var array = Enumerable.Range(10, 10).ToUnmanagedArray()) {
                Assert(array.IndexOf(14) == 4);
                Assert(array.Contains(179) == false);
                Assert(array.Contains(16) == true);
                var copy = new int[array.Length + 5];
                array.CopyTo(copy, 5);
                Assert(copy.Skip(5).SequenceEqual(array));
                using(var array2 = new UnmanagedArray<int>(array.Length)) {
                    array2.CopyFrom(array.Ptr, 2, 8);
                    Assert(array2.Skip(2).SequenceEqual(array.Take(8)));
                }
            }

            var data = new TestStruct()
            {
                A = 10,
                B = 5,
                C = 90,
                D = new TestSubStruct()
                {
                    A = 32,
                    B = 50,
                    C = 0xAABBCCDDEEFF0011,
                }
            };
            using(var array = UnmanagedArray<uint>.CreateFromStruct(data)) {
                Assert(array[0] == 10);
                Assert(array[1] == 5);
                Assert(array[2] == 90);
                Assert(array[3] == 32);
                Assert(array[4] == 50);
                Assert(array[5] == 0xEEFF0011);
                Assert(array[6] == 0xAABBCCDD);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct TestStruct
        {
            [FieldOffset(0)]
            public int A;
            [FieldOffset(4)]
            public int B;
            [FieldOffset(8)]
            public int C;
            [FieldOffset(12)]
            public TestSubStruct D;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct TestSubStruct
        {
            [FieldOffset(0)]
            public int A;
            [FieldOffset(4)]
            public int B;
            [FieldOffset(8)]
            public ulong C;
        }
    }
}
