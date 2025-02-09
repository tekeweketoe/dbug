using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Loader
{
    internal class GameProcess
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        public static Process Process { get; private set; }
        public static IntPtr ProcessHandle { get; private set; }
        public static IntPtr ClientDll { get; private set; }
        private static IntPtr Handle => ProcessHandle; // Add this line

        private static void WriteDebug(string message)
        {
            System.IO.File.AppendAllText("debug.txt", $"{DateTime.Now}: {message}\n");
        }
        public static T ReadMemory<T>(IntPtr address) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = new byte[size];

            IntPtr bytesRead;
            ReadProcessMemory(Handle, address, buffer, size, out bytesRead);

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();

            return result;
        }

        public static void Initialize()
        {
            WriteDebug("Starting game process initialization");
            WriteDebug($"Running in 64-bit mode: {Environment.Is64BitProcess}");

            Process[] processes = Process.GetProcessesByName("cs2");
            WriteDebug($"Found {processes.Length} CS2 processes");

            if (processes.Length > 0)
            {
                Process = processes[0];
                ProcessHandle = Process.Handle;
                WriteDebug($"Selected CS2 process ID: {Process.Id}");

                foreach (ProcessModule module in Process.Modules)
                {
                    WriteDebug($"Checking module: {module.ModuleName}");
                    if (module.ModuleName == "client.dll")
                    {
                        ClientDll = module.BaseAddress;
                        WriteDebug($"Found client.dll at: {(long)ClientDll:X}");
                    }
                }
            }
        }

        public static bool IsRunning()
        {
            bool running = Process != null && !Process.HasExited;
            WriteDebug($"Game process running status: {running}");
            return running;
        }
    }
}
