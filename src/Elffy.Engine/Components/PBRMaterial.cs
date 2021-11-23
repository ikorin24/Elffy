#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Elffy.Components
{
    [DebuggerDisplay("{DebugView,nq}")]
    [Obsolete("Don't use", true)]
    public sealed class PbrMaterial : IComponent
    {
        private PbrMaterialData _data;

        public ref Color3 Albedo => ref _data.Albedo;

        public ref float Metallic => ref _data.Metallic;

        public ref float Roughness => ref _data.Roughness;

        public ref Color3 Emit => ref _data.Emit;

        public ref PbrMaterialData Data => ref _data;

        public PbrMaterial(in Color3 albedo, float metallic, float roughness, in Color3 emit)
        {
            _data = new PbrMaterialData(albedo, metallic, roughness, emit);
        }

        public PbrMaterial(in PbrMaterialData data)
        {
            _data = data;
        }

        public void OnAttached(ComponentOwner owner)
        {
        }

        public void OnDetached(ComponentOwner owner)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string DebugView => _data.DebugView;
    }

    [DebuggerDisplay("{DebugView,nq}")]
    [StructLayout(LayoutKind.Explicit)]
    [Obsolete("Don't use", true)]
    public struct PbrMaterialData : IEquatable<PbrMaterialData>
    {
        [FieldOffset(0)]
        public Color3 Albedo;
        [FieldOffset(12)]
        public float Metallic;
        [FieldOffset(16)]
        public Color3 Emit;
        [FieldOffset(28)]
        public float Roughness;

        public PbrMaterialData(in Color3 albedo, float metallic, float roughness, in Color3 emit)
        {
            Albedo = albedo;
            Metallic = metallic;
            Emit = emit;
            Roughness = roughness;
        }

        public PbrMaterial ToMaterial() => new PbrMaterial(this);

        public override bool Equals(object? obj) => obj is PbrMaterialData data && Equals(data);

        public bool Equals(PbrMaterialData other) => Albedo.Equals(other.Albedo) && Metallic == other.Metallic && Emit.Equals(other.Emit) && Roughness == other.Roughness;

        public override int GetHashCode() => HashCode.Combine(Albedo, Metallic, Emit, Roughness);

        public static bool operator ==(PbrMaterialData left, PbrMaterialData right) => left.Equals(right);

        public static bool operator !=(PbrMaterialData left, PbrMaterialData right) => !(left == right);

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string DebugView => $"(R:{Albedo.R:N3}, G:{Albedo.G:N3}, B:{Albedo.B:N3}), Metallic={Metallic:N3}, Roughness={Roughness:N3}, Emit=(R:{Emit.R:N3}, G:{Emit.G:N3}, B:{Emit.B:N3})";
    }
}
