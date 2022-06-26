#nullable enable
using Cysharp.Threading.Tasks;

namespace Elffy.Imaging
{
    public delegate void ImageAction(ImageRef image);
    public delegate void ImageAction<in TArg>(ImageRef image, TArg arg);
    public delegate void ReadOnlyImageAction(ReadOnlyImageRef image);
    public delegate void ReadOnlyImageAction<in TArg>(ReadOnlyImageRef image, TArg arg);

    public delegate TResult ImageFunc<out TResult>(ImageRef image);
    public delegate TResult ImageFunc<in TArg, out TResult>(ImageRef image, TArg arg);
    public delegate TResult ReadOnlyImageFunc<out TResult>(ReadOnlyImageRef image);
    public delegate TResult ReadOnlyImageFunc<in TArg, out TResult>(ReadOnlyImageRef image, TArg arg);

    public delegate UniTask AsyncImageAction(ImageRef image);
    public delegate UniTask AsyncImageAction<in TArg>(ImageRef image, TArg arg);
    public delegate UniTask AsyncReadOnlyImageAction(ReadOnlyImageRef image);
    public delegate UniTask AsyncReadOnlyImageAction<in TArg>(ReadOnlyImageRef image, TArg arg);

    public delegate UniTask<TResult> AsyncImageFunc<TResult>(ImageRef image);
    public delegate UniTask<TResult> AsyncImageFunc<in TArg, TResult>(ImageRef image, TArg arg);
    public delegate UniTask<TResult> AsyncReadOnlyImageFunc<TResult>(ReadOnlyImageRef image);
    public delegate UniTask<TResult> AsyncReadOnlyImageFunc<in TArg, TResult>(ReadOnlyImageRef image, TArg arg);
}
