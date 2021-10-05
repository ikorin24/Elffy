#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Elffy.Components
{
    [DebuggerDisplay("{DebugView,nq}")]
    public sealed class PBRMaterial : IComponent
    {
        private PBRMaterialData _data;

        public ref Color3 Albedo => ref _data.Albedo;

        public ref float Metallic => ref _data.Metallic;

        public ref float Roughness => ref _data.Roughness;

        public ref Color3 Emit => ref _data.Emit;

        public ref PBRMaterialData Data => ref _data;

        public PBRMaterial(in Color3 albedo, float metallic, float roughness, in Color3 emit)
        {
            _data = new PBRMaterialData(albedo, metallic, roughness, emit);
        }

        public PBRMaterial(in PBRMaterialData data)
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
    public struct PBRMaterialData : IEquatable<PBRMaterialData>
    {
        [FieldOffset(0)]
        public Color3 Albedo;
        [FieldOffset(12)]
        public float Metallic;
        [FieldOffset(16)]
        public Color3 Emit;
        [FieldOffset(28)]
        public float Roughness;

        public PBRMaterialData(in Color3 albedo, float metallic, float roughness, in Color3 emit)
        {
            Albedo = albedo;
            Metallic = metallic;
            Emit = emit;
            Roughness = roughness;
        }

        public PBRMaterial ToMaterial() => new PBRMaterial(this);

        public override bool Equals(object? obj) => obj is PBRMaterialData data && Equals(data);

        public bool Equals(PBRMaterialData other) => Albedo.Equals(other.Albedo) && Metallic == other.Metallic && Emit.Equals(other.Emit) && Roughness == other.Roughness;

        public override int GetHashCode() => HashCode.Combine(Albedo, Metallic, Emit, Roughness);

        public static bool operator ==(PBRMaterialData left, PBRMaterialData right) => left.Equals(right);

        public static bool operator !=(PBRMaterialData left, PBRMaterialData right) => !(left == right);

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string DebugView => $"(R:{Albedo.R:N3}, G:{Albedo.G:N3}, B:{Albedo.B:N3}), Metallic={Metallic:N3}, Roughness={Roughness:N3}, Emit=(R:{Emit.R:N3}, G:{Emit.G:N3}, B:{Emit.B:N3})";
    }
}
