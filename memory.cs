using System;
using System.Runtime.InteropServices;

namespace Loader
{
    internal class Memory
    {
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        public static T ReadMemory<T>(IntPtr address) where T : struct
        {
            byte[] buffer = new byte[Marshal.SizeOf<T>()];
            ReadProcessMemory(GameProcess.ProcessHandle, address, buffer, buffer.Length, out _);
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
            return result;
        }
    }
}
