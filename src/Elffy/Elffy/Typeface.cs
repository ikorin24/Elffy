#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using SkiaSharp;

namespace Elffy
{
    public sealed class Typeface : IDisposable
    {
        private static readonly Lazy<Typeface> _default = new(() => new(SKTypeface.Default), LazyThreadSafetyMode.ExecutionAndPublication);

        private SKTypeface? _skTypeface;

        public static Typeface Default => _default.Value;

        public unsafe Typeface(Stream stream)
        {
            if(stream is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(stream));
            }

            if(stream.CanSeek && stream.Length <= int.MaxValue) {
                var size = (int)stream.Length;
                using var data = SKData.Create(size);
                stream.Read(new Span<byte>((void*)data.Data, size));
                _skTypeface = SKTypeface.FromData(data, 0);
            }
            else {
                using var data = SKData.Create(stream);
                _skTypeface = SKTypeface.FromData(data, 0);
            }
        }

        public Typeface(SKTypeface skTypeface)
        {
            if(skTypeface is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(skTypeface));
            }
            _skTypeface = skTypeface;
        }

        public SKTypeface GetSKTypeface()
        {
            if(_skTypeface is null) {
                ThrowDisposed();
                [DoesNotReturn] static void ThrowDisposed() => throw new ObjectDisposedException(typeof(Typeface).FullName);
            }
            return _skTypeface;
        }

        ~Typeface() => Dispose(false);

        public Font CreateFont(float size)
        {
            return new Font(this, size);
        }

        public void Dispose()
        {
            if(ReferenceEquals(this, Default)) {
                // Don't dispose default singleton instance
                return;
            }

            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(_skTypeface is null) { return; }

            if(disposing) {
                _skTypeface.Dispose();
                _skTypeface = null;
            }
        }
    }
}
