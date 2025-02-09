using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Loader
{
    internal class ESP
    {
        public bool IsEnabled { get; private set; }

        public void ToggleESP(bool enabled)
        {
            IsEnabled = enabled;
            WriteDebug($"ESP state changed to: {enabled}");
        }

        public void UpdateESP()
        {

            WriteDebug($"UpdateESP called with ESP Enabled: {IsEnabled}");
            if (!IsEnabled)
            {
                WriteDebug("ESP is disabled - early return");
                return;
            }

            if (!GameProcess.IsRunning())
            {
                WriteDebug("Game process not running");
                return;
            }

            WriteDebug("Starting ESP update");
            var clientDll = (IntPtr)0x7FFE946E0000;
            WriteDebug($"Using ClientDLL at: {clientDll:X}");
            DrawESP();
            WriteDebug("ESP update complete");
        }
        private bool IsValidEntity(IntPtr entityPtr)
        {
            try
            {
                // Check base entity validity
                if (entityPtr == IntPtr.Zero || entityPtr.ToInt64() < 0x10000)
                    return false;

                int health = GameProcess.ReadMemory<int>(IntPtr.Add(entityPtr, (int)Offsets.m_iHealth));
                int team = GameProcess.ReadMemory<int>(IntPtr.Add(entityPtr, (int)Offsets.m_iTeamNum));

                // Validate health and team ranges based on observed values
                bool validHealth = health > 0 && health <= 100;
                bool validTeam = team >= 1 && team <= 3;

                WriteDebug($"Entity validation - Health: {health}, Team: {team}, Valid: {validHealth && validTeam}");

                return validHealth && validTeam;
            }
            catch
            {
                return false;
            }
        }


        private void ProcessEntity(IntPtr entityPtr)
        {
            if (!IsValidEntity(entityPtr))
                return;

            int team = GameProcess.ReadMemory<int>(IntPtr.Add(entityPtr, (int)Offsets.m_iTeamNum));
            Vector3 position = GameProcess.ReadMemory<Vector3>(IntPtr.Add(entityPtr, (int)Offsets.m_vecOrigin));

            WriteDebug($"Valid entity found - Team: {team}, Position: {position}");
            // Draw ESP box here
        }
        public void RunESP()
        {
            if (!IsEnabled) return;

            var entityList = (IntPtr)((long)GameProcess.ClientDll + Offsets.dwEntityList);
            var localPlayer = (IntPtr)((long)GameProcess.ClientDll + Offsets.dwLocalPlayer);

            WriteDebug($"Running ESP - EntityList: {entityList:X}, LocalPlayer: {localPlayer:X}");

            ProcessEntities(entityList, localPlayer);
        }

        private void ProcessEntities(IntPtr entityList, IntPtr localPlayer)
        {
            WriteDebug("Processing entities for ESP");
        }

        
        private SharpDX.Direct2D1.Factory factory;
        private WindowRenderTarget renderTarget;
        private SolidColorBrush boxBrush;
        public bool espEnabled = false;

        [StructLayout(LayoutKind.Sequential)]
        public struct ViewMatrix
        {
            public float M11, M12, M13, M14;
            public float M21, M22, M23, M24;
            public float M31, M32, M33, M34;
            public float M41, M42, M43, M44;
        }
        private void CreateBrush()
        {
            if (boxBrush != null)
                boxBrush.Dispose();

            boxBrush = new SolidColorBrush(renderTarget, new RawColor4(0, 1, 0, 1));
        }
        private Overlay overlayWindow;
        public void Initialize()
        {
            overlayWindow = new Overlay();
            overlayWindow.Show();

            factory = new SharpDX.Direct2D1.Factory();

            var properties = new RenderTargetProperties
            {
                Type = RenderTargetType.Hardware,
                PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)
            };

            var hwndProperties = new HwndRenderTargetProperties
            {
                Hwnd = overlayWindow.Handle,
                PixelSize = new Size2(1920, 1080),
                PresentOptions = PresentOptions.Immediately
            };

            renderTarget = new WindowRenderTarget(factory, properties, hwndProperties);
            CreateBrush();
        }
        private void WriteDebug(string message)
        {
            System.IO.File.AppendAllText("debug.txt", $"{DateTime.Now}: {message}\n");
        }

        public void DrawESP()
        {
            if (!espEnabled || !GameProcess.IsRunning() || renderTarget == null || boxBrush == null)
                return;

            renderTarget.BeginDraw();
            renderTarget.Clear(new RawColor4(0, 0, 0, 0));

            var entityList = Memory.ReadMemory<long>((IntPtr)((long)GameProcess.ClientDll + Offsets.dwEntityList));
            var localPlayer = Memory.ReadMemory<long>((IntPtr)((long)GameProcess.ClientDll + Offsets.dwLocalPlayer));
            var localTeam = Memory.ReadMemory<int>(new IntPtr(localPlayer + Offsets.m_iTeamNum));

            for (int i = 1; i < 64; i++)
            {
                var entity = Memory.ReadMemory<long>(new IntPtr(entityList + (i * 0x8)));
                if (entity == 0) continue;

                var health = Memory.ReadMemory<int>(new IntPtr(entity + Offsets.m_iHealth));
                var team = Memory.ReadMemory<int>(new IntPtr(entity + Offsets.m_iTeamNum));

                if (health < 1 || health > 100 || team == localTeam)
                    continue;

                Vector3 position = Memory.ReadMemory<Vector3>(new IntPtr(entity + Offsets.m_vecOrigin));
                Vector3 screen = WorldToScreen(position);

                // Only draw if on screen
                if (screen.X > 0 && screen.X < 1920 && screen.Y > 0 && screen.Y < 1080)
                {
                    float height = 100f;  // Bigger height
                    float width = 40f;   // Wider box

                    renderTarget.DrawRectangle(
                        new RawRectangleF(screen.X - width / 2, screen.Y - height,
                                         screen.X + width / 2, screen.Y),
                        boxBrush,
                        1.5f
                    );
                }
            }

            renderTarget.EndDraw();
        }


        private Vector3 WorldToScreen(Vector3 pos)
        {
            var viewMatrix = Memory.ReadMemory<ViewMatrix>((IntPtr)((long)GameProcess.ClientDll + Offsets.dwViewMatrix));
            float w = viewMatrix.M41 * pos.X + viewMatrix.M42 * pos.Y + viewMatrix.M43 * pos.Z + viewMatrix.M44;

            if (w < 0.01f)
                return new Vector3();

            float x = viewMatrix.M11 * pos.X + viewMatrix.M12 * pos.Y + viewMatrix.M13 * pos.Z + viewMatrix.M14;
            float y = viewMatrix.M21 * pos.X + viewMatrix.M22 * pos.Y + viewMatrix.M23 * pos.Z + viewMatrix.M24;

            return new Vector3(
                (1920 / 2) * (1 + x / w),
                (1080 / 2) * (1 - y / w),
                0
            );
        }
    }
}
