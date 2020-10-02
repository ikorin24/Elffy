#nullable enable
using System.Text;

namespace Elffy.OpenGL.Windowing
{
    internal readonly struct CharInputEventArgs
    {
        /// <summary>Unicode rune value</summary>
        public Rune Unicode { get; }

        internal CharInputEventArgs(uint unicode)
        {
            Unicode = new Rune(unicode);
        }
    }
}
