using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ElffyGame
{
    public static class ResourcePassword
    {
        [DllImport("PG.dll", EntryPoint = "GeneratePassword", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetPasswordFromUnmanaged(out IntPtr ptr);

        public static string GetPassword()
        {
            var passArray = new byte[GetPasswordFromUnmanaged(out var ptr)];
            Marshal.Copy(ptr, passArray, 0, passArray.Length);
            var password = Encoding.UTF8.GetString(passArray);
            return password;
        }
    }
}
