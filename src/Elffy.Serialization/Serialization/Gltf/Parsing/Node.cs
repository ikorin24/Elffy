#nullable enable

namespace Elffy.Serialization.Gltf.Parsing;

internal struct Node
{
    public uint? camera = null;
    public uint[]? children = null;
    public uint? skin = null;
    public Matrix4 matrix = Matrix4.Identity;
    public uint? mesh = null;
    public Quaternion rotation = Quaternion.Identity;
    public Vector3 scale = new Vector3(1, 1, 1);
    public Vector3 translation = new Vector3();
    public float[]? weights = null;
    public U8String? name = null;

    public Node()
    {
    }
}
