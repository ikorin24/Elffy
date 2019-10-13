using System;
using System.Runtime.InteropServices;

namespace Elffy.Platforms.Windows
{
    /// <summary>Windows のメッセージボックスを扱うクラスです</summary>
    public static class MessageBox
    {
        /// <summary>メッセージボックスを表示します</summary>
        /// <param name="text">表示するテキスト</param>
        /// <returns>メッセージボックスの結果</returns>
        public static MessageBoxResult Show(string text)
        {
            if(Platform.PlatformType != PlatformType.Windows) { throw Platform.NewPlatformNotSupportedException(); }
            return (MessageBoxResult)WinMessageBox(IntPtr.Zero, text, "", 0);
        }

        /// <summary>メッセージボックスを表示します</summary>
        /// <param name="text">表示するテキスト</param>
        /// <param name="caption">メッセージボックスのキャプション</param>
        /// <returns>メッセージボックスの結果</returns>
        public static MessageBoxResult Show(string text, string caption)
        {
            if(Platform.PlatformType != PlatformType.Windows) { throw Platform.NewPlatformNotSupportedException(); }
            return (MessageBoxResult)WinMessageBox(IntPtr.Zero, text, caption ?? "", 0);
        }

        /// <summary>メッセージボックスを表示します</summary>
        /// <param name="text">表示するテキスト</param>
        /// <param name="caption">メッセージボックスのキャプション</param>
        /// <param name="type">メッセージボックスのタイプ</param>
        /// <returns>メッセージボックスの結果</returns>
        public static MessageBoxResult Show(string text, string caption, MessageBoxType type)
        {
            if(Platform.PlatformType != PlatformType.Windows) { throw Platform.NewPlatformNotSupportedException(); }
            return (MessageBoxResult)WinMessageBox(IntPtr.Zero, text, caption ?? "", (int)type);
        }

        /// <summary>メッセージボックスを表示します</summary>
        /// <param name="text">表示するテキスト</param>
        /// <param name="caption">メッセージボックスのキャプション</param>
        /// <param name="type">メッセージボックスのタイプ</param>
        /// <param name="icon">メッセージボックスのアイコン</param>
        /// <returns>メッセージボックスの結果</returns>
        public static MessageBoxResult Show(string text, string caption, MessageBoxType type, MessageBoxIcon icon)
        {
            if(Platform.PlatformType != PlatformType.Windows) { throw Platform.NewPlatformNotSupportedException(); }
            return (MessageBoxResult)WinMessageBox(IntPtr.Zero, text, caption, (int)type + (int)icon);
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
