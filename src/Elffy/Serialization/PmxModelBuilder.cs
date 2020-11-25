#nullable enable
using System;
using System.Linq;
using System.IO;
using Elffy.Exceptions;
using Elffy.Core;
using Elffy.Shapes;
using Elffy.Effective;
using StringLiteral;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Serialization
{
    public static class PmxModelBuilder
    {
        //public static Model3D BuildFromPmx(IResourceLoader resourceLoader, string name)
        //{
        //    if(resourceLoader is null) {
        //        ThrowNullArg();
        //        [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(resourceLoader));
        //    }
        //    if(resourceLoader.HasResource(name) == false) {
        //        ThrowNotFound(name);
        //        [DoesNotReturn] static void ThrowNotFound(string name) => throw new ResourceNotFoundException(name);
        //    }

        //    var obj = new NamedResource(resourceLoader, name);
        //    return Model3D.Create(obj, BuldCore);
        //}
    }
}
