﻿#nullable enable
using System;
using Cysharp.Text;

namespace Elffy.Exceptions
{
    public class MemoryLeakException : Exception
    {
        private const string DefaultMessage = "Unmanaged memory or some resources leak !!! Did you forget calling `IDisposable.Dispose()` method ?";

        public Type? TargetType { get; }

        public MemoryLeakException() : base(DefaultMessage)
        {
        }

        public MemoryLeakException(Type targetType) : base(ZString.Concat(DefaultMessage, Environment.NewLine, "Target Type : ", targetType?.FullName))
        {
            TargetType = targetType;
        }
    }
}