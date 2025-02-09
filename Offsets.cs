namespace Loader
{
    internal class Offsets
    {
        public static long dwEntityList { get; set; } = 0x18B3F08;
        public static long dwLocalPlayer { get; set; } = 0x17171D0;
        public static long m_iTeamNum { get; set; } = 0xF4;
        public static long m_iHealth { get; set; } = 0x334;
        public static long m_vecOrigin { get; set; } = 0x1214;
        public static long m_vecViewOffset { get; set; } = 0x108;
        public static long dwViewMatrix { get; set; } = 0x1820130;

        // Default values will be used if scanning fails
        public static void ResetToDefaults()
        {
            dwEntityList = 0x18B3F08;
            dwLocalPlayer = 0x17171D0;
            m_iTeamNum = 0xF4;
            m_iHealth = 0x334;
            m_vecOrigin = 0x1214;
            m_vecViewOffset = 0x108;
            dwViewMatrix = 0x1820130;
        }
    }
}
