#nullable enable
namespace Elffy.Core
{
    /// <summary>interface of terminatable object</summary>
    public interface ITerminatable
    {
        /// <summary>Get object instance is terminated</summary>
        bool IsTerminated { get; }

        /// <summary>Terminate this object instance</summary>
        void Terminate();
    }
}
