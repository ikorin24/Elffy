#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public static unsafe class PrimitiveMeshProvider<TVertex> where TVertex : unmanaged
    {
        public static void LoadPlain(MeshLoadAction<TVertex> action)
        {
            LoadPlain(action, static (action, vertices, indices) => action(vertices, indices));
        }

        public static void LoadPlain<TState>(TState state, MeshLoadAction<TState, TVertex> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            // [indices]
            //
            //        0 ----- 3
            //        |  \    |
            //   y    |    \  |
            //   ^    1 ----- 2
            //   |
            //   + ---> x
            //  /
            // z

            // [uv]
            // OpenGL coordinate of uv is left-bottom based,
            // but many popular format of images (e.g. png) are left-top based.
            // So, I use left-top as uv coordinate.
            //
            //         0 ----- 1  u
            //
            //    0    0 ----- 3
            //    |    |       |
            //    |    |       |
            //    1    1 ----- 2
            //    v

            const float a = 0.5f;
            const int VertexCount = 4;
            const int IndexCount = 6;
            int* indices = stackalloc int[IndexCount] { 0, 1, 2, 0, 2, 3, };

            if(typeof(TVertex) == typeof(Vertex)) {
                ForVertex(state, action, indices);
            }
            else if(typeof(TVertex) == typeof(VertexSlim)) {
                ForVertexSlim(state, action, indices);
            }
            else {
                ForOthers(state, action, indices);
            }
            return;

            static void ForVertex(TState state, MeshLoadAction<TState, TVertex> action, int* indices)
            {
                Debug.Assert(typeof(TVertex) == typeof(Vertex));
                Vertex* vertices = stackalloc Vertex[VertexCount]
                {
                    new(new(-a, a, 0f), new(0f, 0f, 1f), new(0f, 0f)),
                    new(new(-a, -a, 0f), new(0f, 0f, 1f), new(0f, 1f)),
                    new(new(a, -a, 0f), new(0f, 0f, 1f), new(1f, 1f)),
                    new(new(a, a, 0f), new(0f, 0f, 1f), new(1f, 0f)),
                };
                action.Invoke(state, new ReadOnlySpan<TVertex>(vertices, VertexCount), new ReadOnlySpan<int>(indices, IndexCount));
            }

            static void ForVertexSlim(TState state, MeshLoadAction<TState, TVertex> action, int* indices)
            {
                Debug.Assert(typeof(TVertex) == typeof(VertexSlim));
                VertexSlim* vertices = stackalloc VertexSlim[VertexCount]
                {
                    new(new(-a, a, 0f), new(0f, 0f)),
                    new(new(-a, -a, 0f), new(0f, 1f)),
                    new(new(a, -a, 0f), new(1f, 1f)),
                    new(new(a, a, 0f), new(1f, 0f)),
                };
                action.Invoke(state, new ReadOnlySpan<TVertex>(vertices, VertexCount), new ReadOnlySpan<int>(indices, IndexCount));
            }

            static void ForOthers(TState state, MeshLoadAction<TState, TVertex> action, int* indices)
            {
                if(VertexMarshalHelper.TryGetVertexTypeData(typeof(TVertex), out var typeData) == false) {
                    ThrowInvalidVertexType();
                }
                TVertex* vertices = stackalloc TVertex[VertexCount];
                var verticesSpan = new Span<TVertex>(vertices, VertexCount);
                verticesSpan.Clear();

                if(typeData.TryGetField(VertexSpecialField.Position, out var pos) == false) {
                    ThrowInvalidVertexType();
                }
                else {
                    var posOffset = pos.ByteOffset;
                    *(Vector3*)(((byte*)(vertices + 0)) + posOffset) = new(-a, a, 0f);
                    *(Vector3*)(((byte*)(vertices + 1)) + posOffset) = new(-a, -a, 0f);
                    *(Vector3*)(((byte*)(vertices + 2)) + posOffset) = new(a, -a, 0f);
                    *(Vector3*)(((byte*)(vertices + 3)) + posOffset) = new(a, a, 0f);
                }

                if(typeData.TryGetField(VertexSpecialField.Normal, out var normal)) {
                    var normalOffset = normal.ByteOffset;
                    *(Vector3*)(((byte*)(vertices + 0)) + normalOffset) = new(0f, 0f, 1f);
                    *(Vector3*)(((byte*)(vertices + 1)) + normalOffset) = new(0f, 0f, 1f);
                    *(Vector3*)(((byte*)(vertices + 2)) + normalOffset) = new(0f, 0f, 1f);
                    *(Vector3*)(((byte*)(vertices + 3)) + normalOffset) = new(0f, 0f, 1f);
                }
                if(typeData.TryGetField(VertexSpecialField.UV, out var uv)) {
                    var uvOffset = uv.ByteOffset;
                    *(Vector2*)(((byte*)(vertices + 0)) + uvOffset) = new(0, 0);
                    *(Vector2*)(((byte*)(vertices + 1)) + uvOffset) = new(0, 1);
                    *(Vector2*)(((byte*)(vertices + 2)) + uvOffset) = new(1, 1);
                    *(Vector2*)(((byte*)(vertices + 3)) + uvOffset) = new(1, 0);
                }
                action.Invoke(state, verticesSpan, new ReadOnlySpan<int>(indices, IndexCount));
            }
        }

        public static void LoadSphere(MeshLoadAction<TVertex> action)
        {
            LoadSphere(action, static (action, vertices, indices) => action(vertices, indices));
        }

        public static void LoadSphere<TState>(TState state, MeshLoadAction<TState, TVertex> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            const float R = 0.5f;
            const int A = 32;
            const int B = 16;

            if(VertexMarshalHelper.TryGetVertexTypeData(typeof(TVertex), out var typeData) == false) {
                ThrowInvalidVertexType();
            }

            if(typeData.TryGetField(VertexSpecialField.Position, out var posField) == false) {
                ThrowInvalidVertexType();
            }
            typeData.TryGetField(VertexSpecialField.UV, out var uvField);
            typeData.TryGetField(VertexSpecialField.Normal, out var normalField);

            using var vertices = new UnsafeRawArray<TVertex>((B + 1) * (A + 1), false);
            using var indices = new UnsafeRawArray<int>(B * A * 6, false);

            for(int y = 0; y < B + 1; y++) {
                var phi = MathF.PI / 2 - MathF.PI / B * y;
                for(int x = 0; x < A + 1; x++) {
                    var theta = MathF.PI - MathF.PI * 2 / A * x;
                    var (sinPhi, cosPhi) = MathF.SinCos(phi);
                    var (sinTheta, cosTheta) = MathF.SinCos(theta);
                    var pos = new Vector3(cosPhi * cosTheta, sinPhi, cosPhi * sinTheta);
                    var uv = new Vector2((float)x / A, (float)y / B);

                    ref var v = ref vertices[(A + 1) * y + x];
                    if(typeof(TVertex) == typeof(Vertex)) {
                        Unsafe.As<TVertex, Vertex>(ref v) = new Vertex
                        {
                            Position = pos * R,
                            Normal = pos,
                            UV = uv,
                        };
                    }
                    else if(typeof(TVertex) == typeof(VertexSlim)) {
                        Unsafe.As<TVertex, VertexSlim>(ref v) = new VertexSlim
                        {
                            Position = pos * R,
                            UV = uv,
                        };
                    }
                    else {
                        v = default;
                        posField.GetRef<TVertex, Vector3>(ref v) = pos * R;
                        if(uvField is not null) {
                            uvField.GetRef<TVertex, Vector2>(ref v) = uv;
                        }
                        if(normalField is not null) {
                            normalField.GetRef<TVertex, Vector3>(ref v) = pos;
                        }
                    }
                }
            }
            for(int y = 0; y < B; y++) {
                for(int x = 0; x < A; x++) {
                    var k = (A * y + x) * 6;

                    var lt = (A + 1) * y + x;
                    var lb = (A + 1) * (y + 1) + x;
                    var rb = (A + 1) * (y + 1) + (x + 1) % (A + 1);
                    var rt = (A + 1) * y + (x + 1) % (A + 1);

                    indices[k] = lt;
                    indices[k + 1] = lb;
                    indices[k + 2] = rb;
                    indices[k + 3] = lt;
                    indices[k + 4] = rb;
                    indices[k + 5] = rt;
                }
            }

            action.Invoke(state, vertices.AsSpan(), indices.AsSpan());
        }

        [DoesNotReturn]
        private static void ThrowInvalidVertexType() => throw new InvalidOperationException($"The type is not supported vertex type. (Type = {typeof(TVertex).FullName})");
    }

    public static class PrimitiveMeshProvider
    {
        public static void LoadArrow(MeshLoadAction<Vertex> action)
        {
            LoadArrow(action, static (action, v, i) => action(v, i));
        }

        public static void LoadArrow<TState>(TState state, MeshLoadAction<TState, Vertex> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            // vertices: 32 * 86 == 2752 [bytes]
            // indices:  4 * 240 == 960  [bytes]
            // ---------------------------------
            // total:               3712 [bytes]

            ReadOnlySpan<Vertex> vertices = stackalloc Vertex[86]
            {
                new Vertex(new Vector3(0.810427f, -5.96046E-08f, -0.0855746f), new Vector3(-1f, 0f, 4.40883E-08f), new Vector2(0.576401f, 0.727186f)),
                new Vertex(new Vector3(0.810427f, -5.96046E-08f, -0.0855746f), new Vector3(0.411431f, -3.04823E-07f, -0.911441f), new Vector2(0.446209f, 0.749336f)),
                new Vertex(new Vector3(0.810427f, -0.032748f, -0.0790607f), new Vector3(-1f, 8.40777E-07f, -6.20573E-07f), new Vector2(0.615504f, 0.708984f)),
                new Vertex(new Vector3(0.810427f, -0.032748f, -0.0790607f), new Vector3(0.411431f, -0.348794f, -0.842061f), new Vector2(0.4082f, 0.72895f)),
                new Vertex(new Vector3(0.810427f, -0.0605105f, -0.0605104f), new Vector3(-1f, 4.92515E-07f, 1.99398E-06f), new Vector2(0.658595f, 0.707132f)),
                new Vertex(new Vector3(0.810427f, -0.0605105f, -0.0605104f), new Vector3(0.411431f, -0.644485f, -0.644487f), new Vector2(0.367418f, 0.714907f)),
                new Vertex(new Vector3(0.810427f, -0.0790607f, -0.032748f), new Vector3(-1f, -3.48261E-07f, -2.97773E-12f), new Vector2(0.699116f, 0.721911f)),
                new Vertex(new Vector3(0.810427f, -0.0790607f, -0.032748f), new Vector3(0.411432f, -0.842061f, -0.348794f), new Vector2(0.324915f, 0.707572f)),
                new Vertex(new Vector3(0.810427f, -0.0855747f, -6.62261E-09f), new Vector3(-1f, 0f, -1.65429E-13f), new Vector2(0.730896f, 0.751072f)),
                new Vertex(new Vector3(0.810427f, -0.0855747f, -6.62261E-09f), new Vector3(0.411431f, -0.911441f, -1.01608E-08f), new Vector2(0.281786f, 0.707132f)),
                new Vertex(new Vector3(0.810427f, -0.0790607f, 0.032748f), new Vector3(-1f, -3.48261E-07f, 2.15058E-12f), new Vector2(0.749099f, 0.790174f)),
                new Vertex(new Vector3(0.810427f, -0.0790607f, 0.032748f), new Vector3(0.411432f, -0.842061f, 0.348793f), new Vector2(0.239142f, 0.713599f)),
                new Vertex(new Vector3(0.810427f, -0.0605105f, 0.0605104f), new Vector3(-1f, 4.92515E-07f, -2.65864E-06f), new Vector2(0.750951f, 0.833266f)),
                new Vertex(new Vector3(0.810427f, -0.0605105f, 0.0605104f), new Vector3(0.411431f, -0.644485f, 0.644487f), new Vector2(0.198082f, 0.726806f)),
                new Vertex(new Vector3(0.810427f, -0.032748f, 0.0790606f), new Vector3(-1f, 8.40777E-07f, -0f), new Vector2(0.736172f, 0.873787f)),
                new Vertex(new Vector3(0.810427f, -0.032748f, 0.0790606f), new Vector3(0.411431f, -0.348793f, 0.842062f), new Vector2(0.159664f, 0.746414f)),
                new Vertex(new Vector3(0.810427f, -5.96046E-08f, 0.0855746f), new Vector3(-1f, 0f, 4.40882E-08f), new Vector2(0.707011f, 0.905567f)),
                new Vertex(new Vector3(0.810427f, -5.96046E-08f, 0.0855746f), new Vector3(0.411431f, -8.12862E-08f, 0.911441f), new Vector2(0.12488f, 0.771916f)),
                new Vertex(new Vector3(0.810427f, 0.0327479f, 0.0790606f), new Vector3(-1f, 0f, 4.40883E-08f), new Vector2(0.667909f, 0.923769f)),
                new Vertex(new Vector3(0.810427f, 0.0327479f, 0.0790606f), new Vector3(0.411431f, 0.348793f, 0.842062f), new Vector2(0.0946241f, 0.802655f)),
                new Vertex(new Vector3(0.810427f, 0.0605103f, 0.0605104f), new Vector3(-1f, 0f, -1.32932E-06f), new Vector2(0.624817f, 0.925622f)),
                new Vertex(new Vector3(0.810427f, 0.0605103f, 0.0605104f), new Vector3(0.411431f, 0.644485f, 0.644487f), new Vector2(0.069677f, 0.83784f)),
                new Vertex(new Vector3(0.810427f, 0.0790606f, 0.032748f), new Vector3(-1f, 0f, 1.32932E-06f), new Vector2(0.584296f, 0.910843f)),
                new Vertex(new Vector3(0.810427f, 0.0790606f, 0.032748f), new Vector3(0.411431f, 0.842062f, 0.348793f), new Vector2(0.0506814f, 0.876563f)),
                new Vertex(new Vector3(0.810427f, 0.0855746f, -1.13837E-08f), new Vector3(-1f, 0f, 2.65865E-06f), new Vector2(0.552516f, 0.881682f)),
                new Vertex(new Vector3(0.810427f, 0.0855746f, -1.13837E-08f), new Vector3(0.411431f, 0.911441f, -2.09312E-06f), new Vector2(0.0381267f, 0.917827f)),
                new Vertex(new Vector3(0.810427f, 0.0855746f, -1.13837E-08f), new Vector3(0.411431f, 0.911441f, -2.09312E-06f), new Vector2(0.552516f, 0.881682f)),
                new Vertex(new Vector3(0.810427f, 0.0790606f, -0.032748f), new Vector3(-1f, 0f, -0f), new Vector2(0.534314f, 0.842579f)),
                new Vertex(new Vector3(0.810427f, 0.0790606f, -0.032748f), new Vector3(0.411431f, 0.842062f, -0.348794f), new Vector2(0.534314f, 0.842579f)),
                new Vertex(new Vector3(0.810427f, 0.0605103f, -0.0605104f), new Vector3(-1f, 0f, -0f), new Vector2(0.532461f, 0.799487f)),
                new Vertex(new Vector3(0.810427f, 0.0605103f, -0.0605104f), new Vector3(0.411431f, 0.644486f, -0.644486f), new Vector2(0.510089f, 0.806893f)),
                new Vertex(new Vector3(0.810427f, 0.0327479f, -0.0790607f), new Vector3(-1f, 0f, -0f), new Vector2(0.54724f, 0.758967f)),
                new Vertex(new Vector3(0.810427f, 0.0327479f, -0.0790607f), new Vector3(0.411431f, 0.348793f, -0.842062f), new Vector2(0.480467f, 0.775543f)),
                new Vertex(new Vector3(1f, 0f, -0f), new Vector3(1f, -1.31699E-06f, -1.37425E-06f), new Vector2(0.300619f, 0.97515f)),
                new Vertex(new Vector3(0f, -1.20388E-07f, -0.0274165f), new Vector3(-1f, 0f, -0f), new Vector2(0.048283f, 0.380282f)),
                new Vertex(new Vector3(0f, -1.20388E-07f, -0.0274165f), new Vector3(-1.48376E-09f, 9.67222E-08f, -1f), new Vector2(0.0948352f, 0.163813f)),
                new Vertex(new Vector3(0.817921f, -1.78814E-07f, -0.0274165f), new Vector3(-1.48376E-09f, 8.705E-08f, -1f), new Vector2(0.979837f, 0.165766f)),
                new Vertex(new Vector3(0f, -0.010492f, -0.0253295f), new Vector3(-1f, 0f, -0f), new Vector2(0.0340118f, 0.37437f)),
                new Vertex(new Vector3(0f, -0.010492f, -0.0253295f), new Vector3(-2.34753E-08f, -0.382684f, -0.923879f), new Vector2(0.0948352f, 0.17926f)),
                new Vertex(new Vector3(0.817921f, -0.010492f, -0.0253295f), new Vector3(-2.34753E-08f, -0.382684f, -0.923879f), new Vector2(0.979837f, 0.181213f)),
                new Vertex(new Vector3(0f, -0.0193865f, -0.0193864f), new Vector3(-1f, 6.27672E-08f, -6.27676E-08f), new Vector2(0.0230891f, 0.363448f)),
                new Vertex(new Vector3(0f, -0.0193865f, -0.0193864f), new Vector3(-3.73061E-08f, -0.707107f, -0.707107f), new Vector2(0.0948352f, 0.194706f)),
                new Vertex(new Vector3(0.817921f, -0.0193865f, -0.0193864f), new Vector3(-3.73061E-08f, -0.707107f, -0.707107f), new Vector2(0.979837f, 0.19666f)),
                new Vertex(new Vector3(-1.86265E-09f, -0.0253297f, -0.0104918f), new Vector3(-1f, 9.67362E-08f, -5.60103E-08f), new Vector2(0.0171778f, 0.349176f)),
                new Vertex(new Vector3(-1.86265E-09f, -0.0253297f, -0.0104918f), new Vector3(-4.15454E-08f, -0.923879f, -0.382684f), new Vector2(0.0948352f, 0.210154f)),
                new Vertex(new Vector3(0.817921f, -0.0253297f, -0.0104918f), new Vector3(-4.15454E-08f, -0.923879f, -0.382684f), new Vector2(0.979837f, 0.212107f)),
                new Vertex(new Vector3(-1.86265E-09f, -0.0274166f, 2.24437E-08f), new Vector3(-1f, 6.79385E-08f, -2.58483E-15f), new Vector2(0.0171778f, 0.333729f)),
                new Vertex(new Vector3(-1.86265E-09f, -0.0274166f, 2.24437E-08f), new Vector3(-4.06976E-08f, -1f, -3.86889E-08f), new Vector2(0.0948352f, 0.2256f)),
                new Vertex(new Vector3(0.817921f, -0.0274166f, 2.24437E-08f), new Vector3(-4.06976E-08f, -1f, -3.86889E-08f), new Vector2(0.979837f, 0.227554f)),
                new Vertex(new Vector3(-1.86265E-09f, -0.0253297f, 0.0104919f), new Vector3(-1f, 9.67363E-08f, 5.60104E-08f), new Vector2(0.0230892f, 0.319458f)),
                new Vertex(new Vector3(-1.86265E-09f, -0.0253297f, 0.0104919f), new Vector3(-4.15454E-08f, -0.923879f, 0.382684f), new Vector2(0.0948352f, 0.241048f)),
                new Vertex(new Vector3(0.817921f, -0.0253297f, 0.0104919f), new Vector3(-4.15454E-08f, -0.923879f, 0.382684f), new Vector2(0.979837f, 0.243001f)),
                new Vertex(new Vector3(0f, -0.0193865f, 0.0193864f), new Vector3(-1f, 6.27672E-08f, 6.27675E-08f), new Vector2(0.0340119f, 0.308536f)),
                new Vertex(new Vector3(0f, -0.0193865f, 0.0193864f), new Vector3(-3.73061E-08f, -0.707106f, 0.707107f), new Vector2(0.0948352f, 0.256495f)),
                new Vertex(new Vector3(0.817921f, -0.0193865f, 0.0193864f), new Vector3(-3.73061E-08f, -0.707106f, 0.707107f), new Vector2(0.979837f, 0.258448f)),
                new Vertex(new Vector3(0f, -0.010492f, 0.0253295f), new Vector3(-1f, 0f, -0f), new Vector2(0.048283f, 0.302624f)),
                new Vertex(new Vector3(0f, -0.010492f, 0.0253295f), new Vector3(-2.31574E-08f, -0.382684f, 0.923879f), new Vector2(0.0948352f, 0.271942f)),
                new Vertex(new Vector3(0.817921f, -0.010492f, 0.0253295f), new Vector3(-2.31574E-08f, -0.382684f, 0.923879f), new Vector2(0.979837f, 0.273895f)),
                new Vertex(new Vector3(0f, -1.24528E-07f, 0.0274165f), new Vector3(-1f, 0f, -0f), new Vector2(0.0637301f, 0.302624f)),
                new Vertex(new Vector3(0f, -1.24528E-07f, 0.0274165f), new Vector3(-1.48376E-09f, 3.86889E-08f, 1f), new Vector2(0.0948352f, 0.287389f)),
                new Vertex(new Vector3(0.817921f, -1.78814E-07f, 0.0274165f), new Vector3(-1.48376E-09f, 2.90167E-08f, 1f), new Vector2(0.979837f, 0.289342f)),
                new Vertex(new Vector3(0f, 0.0104917f, 0.0253295f), new Vector3(-1f, 0f, -0f), new Vector2(0.0780011f, 0.308536f)),
                new Vertex(new Vector3(0f, 0.0104917f, 0.0253295f), new Vector3(1.65864E-08f, 0.382683f, 0.92388f), new Vector2(0.0948352f, 0.302836f)),
                new Vertex(new Vector3(0.817921f, 0.0104917f, 0.0253295f), new Vector3(1.65864E-08f, 0.382683f, 0.92388f), new Vector2(0.979837f, 0.304789f)),
                new Vertex(new Vector3(0f, 0.0193863f, 0.0193864f), new Vector3(-1f, 6.27674E-08f, -6.27669E-08f), new Vector2(0.0889238f, 0.319458f)),
                new Vertex(new Vector3(0f, 0.0193863f, 0.0193864f), new Vector3(3.47625E-08f, 0.707106f, 0.707107f), new Vector2(0.0948351f, 0.318282f)),
                new Vertex(new Vector3(0.817921f, 0.0193862f, 0.0193864f), new Vector3(3.47626E-08f, 0.707106f, 0.707107f), new Vector2(0.979837f, 0.320236f)),
                new Vertex(new Vector3(1.86265E-09f, 0.0253294f, 0.0104919f), new Vector3(-1f, 9.67372E-08f, -5.60102E-08f), new Vector2(0.0948351f, 0.33373f)),
                new Vertex(new Vector3(1.86265E-09f, 0.0253294f, 0.0104919f), new Vector3(5.42634E-08f, 0.923879f, 0.382684f), new Vector2(0.0948351f, 0.33373f)),
                new Vertex(new Vector3(0.817921f, 0.0253294f, 0.0104919f), new Vector3(5.42634E-08f, 0.923879f, 0.382684f), new Vector2(0.979837f, 0.335683f)),
                new Vertex(new Vector3(1.86265E-09f, 0.0274164f, 2.09183E-08f), new Vector3(-1f, 6.79391E-08f, -6.46208E-16f), new Vector2(0.0948351f, 0.349177f)),
                new Vertex(new Vector3(1.86265E-09f, 0.0274164f, 2.09183E-08f), new Vector3(6.10463E-08f, 1f, -7.73778E-08f), new Vector2(0.0948351f, 0.349177f)),
                new Vertex(new Vector3(1.86265E-09f, 0.0274164f, 2.09183E-08f), new Vector3(6.10463E-08f, 1f, -7.73778E-08f), new Vector2(0.0948352f, 0.102024f)),
                new Vertex(new Vector3(0.817921f, 0.0274163f, 2.09183E-08f), new Vector3(6.10463E-08f, 1f, -7.73778E-08f), new Vector2(0.979837f, 0.35113f)),
                new Vertex(new Vector3(0.817921f, 0.0274163f, 2.09183E-08f), new Vector3(6.10463E-08f, 1f, -7.73778E-08f), new Vector2(0.979837f, 0.103978f)),
                new Vertex(new Vector3(1.86265E-09f, 0.0253294f, -0.0104918f), new Vector3(-1f, 9.67371E-08f, 5.60101E-08f), new Vector2(0.0889238f, 0.363448f)),
                new Vertex(new Vector3(1.86265E-09f, 0.0253294f, -0.0104918f), new Vector3(5.34156E-08f, 0.923879f, -0.382684f), new Vector2(0.0948353f, 0.117471f)),
                new Vertex(new Vector3(0.817921f, 0.0253294f, -0.0104918f), new Vector3(5.34156E-08f, 0.923879f, -0.382684f), new Vector2(0.979837f, 0.119425f)),
                new Vertex(new Vector3(0f, 0.0193863f, -0.0193864f), new Vector3(-1f, 6.27674E-08f, 6.2767E-08f), new Vector2(0.0780011f, 0.37437f)),
                new Vertex(new Vector3(0f, 0.0193863f, -0.0193864f), new Vector3(3.39147E-08f, 0.707107f, -0.707106f), new Vector2(0.0948353f, 0.132918f)),
                new Vertex(new Vector3(0.817921f, 0.0193862f, -0.0193864f), new Vector3(3.39147E-08f, 0.707107f, -0.707106f), new Vector2(0.979837f, 0.134872f)),
                new Vertex(new Vector3(0f, 0.0104917f, -0.0253295f), new Vector3(-1f, 0f, -0f), new Vector2(0.0637299f, 0.380282f)),
                new Vertex(new Vector3(0f, 0.0104917f, -0.0253295f), new Vector3(1.69043E-08f, 0.382683f, -0.92388f), new Vector2(0.0948352f, 0.148365f)),
                new Vertex(new Vector3(0.817921f, 0.0104917f, -0.0253295f), new Vector3(1.69043E-08f, 0.382683f, -0.92388f), new Vector2(0.979837f, 0.150319f)),
                new Vertex(new Vector3(0f, 0f, -0f), new Vector3(-1f, 4.83684E-08f, 4.52346E-15f), new Vector2(0.0560067f, 0.341453f)),
                new Vertex(new Vector3(0.810427f, 0f, -1.13837E-08f), new Vector3(-1f, 1.23129E-07f, 9.4105E-08f), new Vector2(0.641706f, 0.816377f)),
            };
            ReadOnlySpan<int> indices = stackalloc int[240] { 1, 33, 3, 3, 33, 5, 5, 33, 7, 7, 33, 9, 9, 33, 11, 11, 33, 13, 13, 33, 15, 15, 33, 17, 17, 33, 19, 19, 33, 21, 21, 33, 23, 23, 33, 25, 26, 33, 28, 28, 33, 30, 24, 85, 22, 30, 33, 32, 32, 33, 1, 35, 36, 39, 35, 39, 38, 38, 39, 42, 38, 42, 41, 41, 42, 45, 41, 45, 44, 44, 45, 48, 44, 48, 47, 47, 48, 51, 47, 51, 50, 50, 51, 54, 50, 54, 53, 53, 54, 57, 53, 57, 56, 56, 57, 60, 56, 60, 59, 59, 60, 63, 59, 63, 62, 62, 63, 66, 62, 66, 65, 65, 66, 69, 65, 69, 68, 68, 69, 73, 68, 73, 71, 72, 74, 77, 72, 77, 76, 76, 77, 80, 76, 80, 79, 79, 80, 83, 79, 83, 82, 82, 83, 36, 82, 36, 35, 58, 84, 55, 61, 84, 58, 64, 84, 61, 67, 84, 64, 70, 84, 67, 75, 84, 70, 78, 84, 75, 81, 84, 78, 34, 84, 81, 37, 84, 34, 40, 84, 37, 43, 84, 40, 46, 84, 43, 49, 84, 46, 52, 84, 49, 55, 84, 52, 22, 85, 20, 20, 85, 18, 18, 85, 16, 16, 85, 14, 14, 85, 12, 12, 85, 10, 10, 85, 8, 8, 85, 6, 6, 85, 4, 4, 85, 2, 2, 85, 0, 0, 85, 31, 31, 85, 29, 29, 85, 27, 27, 85, 24 };
            action.Invoke(state, vertices, indices);
        }
    }

    public delegate void MeshLoadAction<TVertex>(ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged;
    public delegate void MeshLoadAction<TState, TVertex>(TState state, ReadOnlySpan<TVertex> vertices, ReadOnlySpan<int> indices) where TVertex : unmanaged;
}
