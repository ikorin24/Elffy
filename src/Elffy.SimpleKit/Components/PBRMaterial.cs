#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Elffy.Core;

namespace Elffy.Components
{
    [DebuggerDisplay("{DebugView,nq}")]
    public sealed class PBRMaterial : IComponent
    {
        private PBRMaterialData _data;

        public ref Color3 Albedo => ref _data.Albedo;

        public ref Half Metallic => ref _data.Metallic;

        public ref Half Roughness => ref _data.Roughness;

        public ref PBRMaterialData Data => ref _data;

        public PBRMaterial(in Color3 albedo, float metallic, float roughness)
        {
            _data = new PBRMaterialData(albedo, metallic, roughness);
        }

        public PBRMaterial(in Color3 albedo, Half metallic, Half roughness)
        {
            _data = new PBRMaterialData(albedo, metallic, roughness);
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
        internal string DebugView => $"{nameof(PBRMaterial)} [{_data.DebugView}]";
    }

    [DebuggerDisplay("{DebugView,nq}")]
    [StructLayout(LayoutKind.Explicit)]
    public struct PBRMaterialData : IEquatable<PBRMaterialData>
    {
        [FieldOffset(0)]
        public Color3 Albedo;
        [FieldOffset(12)]
        public Half Metallic;
        [FieldOffset(14)]
        public Half Roughness;

        public PBRMaterialData(in Color3 albedo, float metallic, float roughness)
        {
            Albedo = albedo;
            Metallic = (Half)metallic;
            Roughness = (Half)roughness;
        }

        public PBRMaterialData(in Color3 albedo, Half metallic, Half roughness)
        {
            Albedo = albedo;
            Metallic = metallic;
            Roughness = roughness;
        }

        public override bool Equals(object? obj) => obj is PBRMaterialData data && Equals(data);

        public bool Equals(PBRMaterialData other) => Albedo.Equals(other.Albedo) && Metallic == other.Metallic && Roughness == other.Roughness;

        public override int GetHashCode() => HashCode.Combine(Albedo, Metallic, Roughness);

        public static bool operator ==(PBRMaterialData left, PBRMaterialData right) => left.Equals(right);

        public static bool operator !=(PBRMaterialData left, PBRMaterialData right) => !(left == right);

        [EditorBrowsable(EditorBrowsableState.Never)]
        internal string DebugView => $"(R={Albedo.R:N3}, G={Albedo.G:N3}, B={Albedo.B:N3}), Metallic={Metallic:N3}, Roughness={Roughness:N3}";
    }
}
