using System;
using System.Runtime.InteropServices;

namespace SnackAttack.Screens
{
    public static class NativeFilePicker
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName(ref OpenFileName ofn);

        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_PATHMUSTEXIST = 0x00000800;
        private const int OFN_NOCHANGEDIR = 0x00000008;

        public static string OpenImageFile()
        {
            var ofn = new OpenFileName();
            ofn.lStructSize = Marshal.SizeOf(ofn);
            ofn.lpstrFilter = "Image Files\0*.png;*.jpg;*.jpeg;*.bmp;*.webp\0All Files\0*.*\0";
            ofn.lpstrFile = new string(new char[260]);
            ofn.nMaxFile = 260;
            ofn.lpstrFileTitle = new string(new char[260]);
            ofn.nMaxFileTitle = 260;
            ofn.lpstrTitle = "Select Dog Photo";
            ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;

            if (GetOpenFileName(ref ofn))
                return ofn.lpstrFile.TrimEnd('\0');

            return string.Empty;
        }
    }
}
