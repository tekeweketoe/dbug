using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Loader
{

    internal class OffsetFinder
    {


        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        private static readonly string[] EntityListSignature = new[] {
            "48 8B 0D ?? ?? ?? ?? 48 89 7C 24 ?? 8B FA C1 EB",
            "48 8B 0D ?? ?? ?? ?? 48 89 7C 24 ?? 8B FA 83 FA"
        };

        public static void FindOffsets()
        {
            Console.WriteLine("Starting offset scan...");
            var process = GameProcess.Process;
            var clientModule = process.Modules.Cast<ProcessModule>()
                .FirstOrDefault(m => m.ModuleName.Equals("client.dll", StringComparison.OrdinalIgnoreCase));

            if (clientModule != null)
            {
                ScanForOffsets(process, clientModule);
            }
        }

        private static void ScanForOffsets(Process process, ProcessModule clientModule)
        {
            byte[] moduleBytes = new byte[clientModule.ModuleMemorySize];
            ReadProcessMemory(process.Handle, clientModule.BaseAddress, moduleBytes, moduleBytes.Length, out _);

            Console.WriteLine("Starting memory scan...");

            // Convert first signature pattern to bytes
            byte[] pattern = StringToBytePattern(EntityListSignature[0]);
            string mask = GenerateMask(EntityListSignature[0]);

            IntPtr result = ScanPattern(moduleBytes, pattern, mask);

            if (result != IntPtr.Zero)
            {
                Console.WriteLine($"Found EntityList at offset: 0x{result.ToInt64():X}");
                // Store this offset for your ESP
                Offsets.dwEntityList = result.ToInt64();
            }
        }

        private static byte[] StringToBytePattern(string pattern)
        {
            return pattern.Split(' ')
                .Select(s => s == "??" ? (byte)0 : Convert.ToByte(s, 16))
                .ToArray();
        }

        private static string GenerateMask(string pattern)
        {
            return string.Join("", pattern.Split(' ').Select(b => b == "??" ? "?" : "x"));
        }

        private static IntPtr ScanPattern(byte[] memory, byte[] pattern, string mask)
        {
            for (int i = 0; i < memory.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (mask[j] != '?' && pattern[j] != memory[i + j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    return new IntPtr(i);
            }
            return IntPtr.Zero;
        }
        private static readonly Dictionary<string, string> OffsetPatterns = new Dictionary<string, string>
{
    { "dwEntityList", "48 8B 0D ?? ?? ?? ?? 48 89 7C 24 ?? 8B FA C1 EB" },
    { "dwLocalPlayer", "48 8D 05 ?? ?? ?? ?? C3 CC CC CC CC CC CC CC 48" },
    { "m_iHealth", "83 B9 ?? ?? ?? ?? 00 7F ?? 8B 91" },
    { "m_iTeamNum", "8B 89 ?? ?? ?? ?? 83 F9 ?? 75 ??" },
    { "m_vecOrigin", "F3 0F 10 89 ?? ?? ?? ?? F3 0F 10 81" }
};

        public static void ScanAllOffsets()
        {
            var process = GameProcess.Process;
            var clientModule = process.Modules.Cast<ProcessModule>()
                .FirstOrDefault(m => m.ModuleName.Equals("client.dll", StringComparison.OrdinalIgnoreCase));

            if (clientModule == null)
                return;

            byte[] moduleBytes = new byte[clientModule.ModuleMemorySize];
            ReadProcessMemory(process.Handle, clientModule.BaseAddress, moduleBytes, moduleBytes.Length, out _);

            foreach (var pattern in OffsetPatterns)
            {
                byte[] searchPattern = StringToBytePattern(pattern.Value);
                string mask = GenerateMask(pattern.Value);
                IntPtr offset = ScanPattern(moduleBytes, searchPattern, mask);

                if (offset != IntPtr.Zero)
                {
                    Console.WriteLine($"Found {pattern.Key} at offset: 0x{offset.ToInt64():X}");

                    switch (pattern.Key)
                    {
                        case "dwEntityList":
                            Offsets.dwEntityList = offset.ToInt64();
                            break;
                        case "dwLocalPlayer":
                            Offsets.dwLocalPlayer = offset.ToInt64();
                            break;
                        case "m_iHealth":
                            Offsets.m_iHealth = offset.ToInt64();
                            break;
                        case "m_iTeamNum":
                            Offsets.m_iTeamNum = offset.ToInt64();
                            break;
                        case "m_vecOrigin":
                            Offsets.m_vecOrigin = offset.ToInt64();
                            break;
                    }
                }
            }
        }
    }
}