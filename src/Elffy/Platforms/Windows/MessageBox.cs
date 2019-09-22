using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Platforms.Windows
{
    public static class MessageBox
    {
        public static MessageBoxResult Show(string text, string caption, MessageBoxType type, MessageBoxIcon icon)
        {
            if(Platform.PlatformType == PlatformType.Windows) {
                var result = WinMessageBox(IntPtr.Zero, text, caption, (int)type + (int)icon);
                return (MessageBoxResult)result;
            }
            else { throw Platform.NewPlatformNotSupportedException(); }
        }

        [DllImport("user32.dll", EntryPoint = "MessageBox", CharSet = CharSet.Unicode)]
        private static extern WinMessageBoxResult WinMessageBox(IntPtr hWnd, 
            [MarshalAs(UnmanagedType.LPWStr), In] string text, 
            [MarshalAs(UnmanagedType.LPWStr), In] string caption, 
            int option);

        private enum WinMessageBoxResult : UInt32
        {
            Ok = 1,
            Cancel = 2,
            Abort = 3,
            Retry = 4,
            Ignore = 5,
            Yes = 6,
            No = 7,
            Close = 8,
            Help = 9,
            TryAgain = 10,
            Continue = 11,
            Timeout = 32000
        }
    }

    [Flags]
    public enum MessageBoxType : UInt64
    {
        Ok = 0x00000000L,
        OkCancel = 0x00000001L,
        YesNo = 0x00000004L,
        YesNoCancel = 0x00000003L,
    }

    [Flags]
    public enum MessageBoxIcon : UInt64
    {
        /// <summary>A stop-sign icon appears in the message box.</summary>
        Error = 0x00000010L,
        /// <summary>An exclamation-point icon appears in the message box.</summary>
        Exclamation = 0x00000030L,
        /// <summary>An icon consisting of a lowercase letter i in a circle appears in the message box.</summary>
        Infomation = 0x00000040L,
    }

    public enum MessageBoxResult
    {
        Ok = 1,
        Cancel = 2,
        Yes = 6,
        No = 7,
        Aborted = -1,
    }
}
