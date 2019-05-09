using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy
{
    public static class ResourceLoader
    {
        private const string RESOURCE_LOADER_DLL = "EllfyResourceLoader.dll";

        [DllImport(RESOURCE_LOADER_DLL, EntryPoint = "GetResourceCount", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetResourceCountFromNative();

        [DllImport(RESOURCE_LOADER_DLL, EntryPoint = "Initialize", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool InitializeFromNative(int resourceCount, [In] ref IntPtr lengthPtr, [In] ref IntPtr positionPtr);


        private static void Initialize()
        {
            var count = GetResourceCountFromNative();
            long[] fileLength = null;
            long[] filePosition = null;
            var lengthArray = new UnmanagedLongArray();
            var positionArray = new UnmanagedLongArray();
            try {
                lengthArray.AllocUnmanaged(count);
                positionArray.AllocUnmanaged(count);
                var result = InitializeFromNative(count, ref lengthArray.Ptr, ref positionArray.Ptr);
                if(result == false) {
                    throw new Exception("Failed in Loading Resource");
                }
                fileLength = lengthArray.ToManagedArray();
                filePosition = positionArray.ToManagedArray();
            }
            finally {
                if(lengthArray.IsUsed) {
                    lengthArray.Free();
                }
                if(positionArray.IsUsed) {
                    positionArray.Free();
                }
            }
        }

        #region struct UnmanagedLongArray
        private struct UnmanagedLongArray
        {
            private static int SIZE_OF_LONG = 8;

            public IntPtr Ptr;
            public int Length;
            public bool IsFree;
            public bool IsNull => Ptr == IntPtr.Zero;
            public bool IsUsed => !IsNull && !IsFree;

            public void AllocUnmanaged(int length)
            {
                var size = length * SIZE_OF_LONG;
                Ptr = Marshal.AllocHGlobal(size);
                Length = length;
                IsFree = false;
            }

            public long[] ToManagedArray()
            {
                var array = new long[Length];
                Marshal.Copy(Ptr, array, 0, array.Length);
                return array;
            }

            public void Free()
            {
                Marshal.FreeHGlobal(Ptr);
                Ptr = IntPtr.Zero;
                IsFree = true;
            }
        }
        #endregion struct UnmanagedLongArray
    }
}
