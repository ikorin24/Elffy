using System;
using Elffy.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ComponentOwnerTest
    {
        [TestMethod]
        public void BasicAPI()
        {
            var owner = new SampleOwner();
            var comp1 = new Component1() { Name = "hoge" };
            var comp2 = new Component2() { Name = "piyo" };

            Assert.IsFalse(owner.HasComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component2>());

            Assert.IsFalse(owner.RemoveComponent<Component1>());
            Assert.IsFalse(owner.RemoveComponent<Component2>());

            Assert.IsFalse(owner.HasComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component2>());

            owner.AddComponent(comp1);
            Assert.IsTrue(owner.HasComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component2>());

            Assert.IsTrue(owner.RemoveComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component2>());

            owner.AddComponent(comp2);
            Assert.IsFalse(owner.HasComponent<Component1>());
            Assert.IsTrue(owner.HasComponent<Component2>());

            owner.AddComponent(comp1);
            Assert.IsTrue(owner.HasComponent<Component1>());
            Assert.IsTrue(owner.HasComponent<Component2>());

            Assert.ThrowsException<ArgumentException>(() => owner.AddComponent(new Component2()));

            var c1 = owner.GetComponent<Component1>();
            Assert.IsNotNull(c1);
            Assert.IsTrue(c1.Name == "hoge");

            var c2 = owner.GetComponent<Component2>();
            Assert.IsNotNull(c2);
            Assert.IsTrue(c2.Name == "piyo");

            Assert.IsFalse(owner.HasComponent<Component3>());
            Assert.IsNull(owner.GetComponent<Component3>());
        }

        class Component1
        {
            public string Name { get; set; }
        }

        class Component2
        {
            public string Name { get; set; }
        }

        class Component3
        {
        }

        class SampleOwner : ComponentOwner
        {
        }
    }
}
