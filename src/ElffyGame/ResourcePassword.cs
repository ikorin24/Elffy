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
            var passLen = GetPasswordFromUnmanaged(out var ptr);
            var passArray = new byte[passLen];
            Marshal.Copy(ptr, passArray, 0, passArray.Length);
            var password = Encoding.ASCII.GetString(passArray);
            return password;
        }
    }
}
