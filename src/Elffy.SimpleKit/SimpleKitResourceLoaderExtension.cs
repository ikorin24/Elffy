#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Imaging;
using Elffy.Shapes;
using System;
using MMDTools.Unmanaged;
using Elffy.Effective;
using System.Drawing;

namespace Elffy
{
    public static class SimpleKitResourceLoaderExtension
    {
        public static UniTask<PmxModel> LoadPmxModelAsync(this IResourceLoader source, string name)
        {
            return UniTask.Run(Load, false);

            PmxModel Load() => LoadPmxModel(source, name);
        }

        public static PmxModel LoadPmxModel(this IResourceLoader source, string name)
        {
            PMXObject? pmx = default;
            try {
                using(var stream = source.GetStream(name)) {
                    pmx = PMXParser.Parse(stream);
                }
                var textureNames = pmx.TextureList.AsSpan();
                var dir = ResourcePath.GetDirectoryName(name);
                var bitmaps = new RefTypeRentMemory<Bitmap>(textureNames.Length);
                var bitmapSpan = bitmaps.Span;

                for(int i = 0; i < bitmapSpan.Length; i++) {
                    using(GetTexturePath(dir, textureNames[i], out var texturePath, out var ext))
                    using(var stream = source.GetStream(texturePath.ToString())) {
                        bitmapSpan[i] = BitmapHelper.StreamToBitmap(stream, ext);
                    }
                }
                return new PmxModel(pmx, bitmaps);
            }
            catch {
                pmx?.Dispose();
                throw;
            }

            #region local func
            static PooledArray<char> GetTexturePath(ReadOnlySpan<char> dir, ReadOnlyRawString name, out Span<char> texturePath, out ReadOnlySpan<char> ext)
            {
                var pooledArray = new PooledArray<char>(dir.Length + 1 + name.GetCharCount());
                try {
                    texturePath = pooledArray.AsSpan();
                    dir.CopyTo(texturePath);
                    texturePath[dir.Length] = '/';
                    name.ToString(texturePath.Slice(dir.Length + 1));
                    texturePath.Replace('\\', '/');
                    ext = ResourcePath.GetExtension(texturePath);

                    return pooledArray;
                }
                catch {
                    pooledArray.Dispose();
                    throw;
                }
            }
            #endregion

        }
    }
}
