#nullable enable
using System;
using Elffy;
using Elffy.Components;
using Xunit;

namespace UnitTest
{
    public class ComponentOwnerTest
    {
        [Fact(DisplayName = "Component Owner API")]
        public void BasicAPI()
        {
            var owner = new SampleOwner();
            var comp1 = new Component1() { Name = "hoge" };
            var comp2 = new Component2() { Name = "piyo" };

            Assert.False(owner.HasComponent<Component1>());
            Assert.False(owner.HasComponent<Component2>());

            // owner's component == [ ]

            Assert.False(owner.RemoveComponent<Component1>());
            Assert.False(owner.RemoveComponent<Component2>());

            // owner's component == [ ]

            Assert.False(owner.HasComponent<Component1>());
            Assert.False(owner.HasComponent<Component2>());

            // owner's component == [ ]

            owner.AddComponent(comp1);
            Assert.True(owner.HasComponent<Component1>());
            Assert.False(owner.HasComponent<Component2>());

            // owner's component == [ comp1{ Name = "hoge" } ]

            Assert.True(owner.RemoveComponent<Component1>());
            Assert.False(owner.HasComponent<Component1>());
            Assert.False(owner.HasComponent<Component2>());

            // owner's component == [ ]

            owner.AddComponent(comp2);
            Assert.False(owner.HasComponent<Component1>());
            Assert.True(owner.HasComponent<Component2>());

            // owner's component == [ comp2{ Name = "piyo" } ]

            owner.AddComponent(comp1);
            Assert.True(owner.HasComponent<Component1>());
            Assert.True(owner.HasComponent<Component2>());

            // owner's component == [ comp1{ Name = "hoge"}, comp2{ Name = "piyo" } ]

            Assert.Throws<ArgumentException>(() => owner.AddComponent(new Component2()));

            // owner's component == [ comp1{ Name = "hoge"}, comp2{ Name = "piyo" } ]

            var c1 = owner.GetComponent<Component1>();
            Assert.NotNull(c1);
            Assert.True(c1.Name == "hoge");

            // owner's component == [ comp1{ Name = "hoge"}, comp2{ Name = "piyo" } ]

            var c2 = owner.GetComponent<Component2>();
            Assert.NotNull(c2);
            Assert.True(c2.Name == "piyo");

            // owner's component == [ comp1{ Name = "hoge"}, comp2{ Name = "piyo" } ]

            Assert.False(owner.HasComponent<Component3>());
            Assert.Throws<InvalidOperationException>(() => owner.GetComponent<Component3>());

            Assert.True(owner.AddOrReplaceComponent(new Component1() { Name = "foo" }, out _));

            // owner's component == [ comp1{ Name = "foo"}, comp2{ Name = "piyo" } ]

            Assert.False(owner.AddOrReplaceComponent(new Component3() { Name = "bar" }, out _));
            Assert.True(owner.GetComponent<Component1>().Name == "foo");
            Assert.True(owner.GetComponent<Component3>().Name == "bar");

            // owner's component == [ comp1{ Name = "foo"}, comp2{ Name = "piyo" }, comp3{ Name = "bar" } ]

            Assert.True(owner.RemoveComponent<Component2>());
            Assert.False(owner.HasComponent<Component2>());
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
