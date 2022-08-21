#nullable enable

namespace Elffy.Serialization.Gltf;

internal struct Node
{
    public int? camera = null;
    public int[]? children = null;
    public int? skin = null;
    public Matrix4 matrix = Matrix4.Identity;
    public int? mesh = null;
    public Quaternion rotation = Quaternion.Identity;
    public Vector3 scale = new Vector3(1, 1, 1);
    public Vector3 translation = new Vector3();
    public float[]? weights = null;
    public U8String? name = null;

    public Node()
    {
    }
}
