#nullable enable
using System;
using Elffy.Core;
using Elffy.Components;
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

            // owner's component == [ ]

            Assert.IsFalse(owner.RemoveComponent<Component1>());
            Assert.IsFalse(owner.RemoveComponent<Component2>());

            // owner's component == [ ]

            Assert.IsFalse(owner.HasComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component2>());

            // owner's component == [ ]

            owner.AddComponent(comp1);
            Assert.IsTrue(owner.HasComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component2>());

            // owner's component == [ comp1{ Name = "hoge" } ]

            Assert.IsTrue(owner.RemoveComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component1>());
            Assert.IsFalse(owner.HasComponent<Component2>());

            // owner's component == [ ]

            owner.AddComponent(comp2);
            Assert.IsFalse(owner.HasComponent<Component1>());
            Assert.IsTrue(owner.HasComponent<Component2>());

            // owner's component == [ comp2{ Name = "piyo" } ]

            owner.AddComponent(comp1);
            Assert.IsTrue(owner.HasComponent<Component1>());
            Assert.IsTrue(owner.HasComponent<Component2>());

            // owner's component == [ comp1{ Name = "hoge"}, comp2{ Name = "piyo" } ]

            Assert.ThrowsException<ArgumentException>(() => owner.AddComponent(new Component2()));

            // owner's component == [ comp1{ Name = "hoge"}, comp2{ Name = "piyo" } ]

            var c1 = owner.GetComponent<Component1>();
            Assert.IsNotNull(c1);
            Assert.IsTrue(c1.Name == "hoge");

            // owner's component == [ comp1{ Name = "hoge"}, comp2{ Name = "piyo" } ]

            var c2 = owner.GetComponent<Component2>();
            Assert.IsNotNull(c2);
            Assert.IsTrue(c2.Name == "piyo");

            // owner's component == [ comp1{ Name = "hoge"}, comp2{ Name = "piyo" } ]

            Assert.IsFalse(owner.HasComponent<Component3>());
            Assert.ThrowsException<InvalidOperationException>(() => owner.GetComponent<Component3>());

            Assert.IsTrue(owner.AddOrReplaceComponent(new Component1() { Name = "foo" }));

            // owner's component == [ comp1{ Name = "foo"}, comp2{ Name = "piyo" } ]

            Assert.IsFalse(owner.AddOrReplaceComponent(new Component3() { Name = "bar" }));
            Assert.IsTrue(owner.GetComponent<Component1>().Name == "foo");
            Assert.IsTrue(owner.GetComponent<Component3>().Name == "bar");

            // owner's component == [ comp1{ Name = "foo"}, comp2{ Name = "piyo" }, comp3{ Name = "bar" } ]

            Assert.IsTrue(owner.RemoveComponent<Component2>());
            Assert.IsFalse(owner.HasComponent<Component2>());
        }

        class Component1 : IComponent
        {
            public string Name { get; set; } = string.Empty;

            public void OnAttached(ComponentOwner owner)
            {
            }

            public void OnDetached(ComponentOwner owner)
            {
            }
        }

        class Component2 : IComponent
        {
            public string Name { get; set; } = string.Empty;
            public void OnAttached(ComponentOwner owner)
            {
            }

            public void OnDetached(ComponentOwner owner)
            {
            }
        }

        class Component3 : IComponent
        {
            public string Name { get; set; } = string.Empty;
            public void OnAttached(ComponentOwner owner)
            {
            }

            public void OnDetached(ComponentOwner owner)
            {
            }
        }

        class SampleOwner : ComponentOwner
        {
        }
    }
}
