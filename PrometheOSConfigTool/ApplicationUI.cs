using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PrometheOSConfigTool
{
    public unsafe class ApplicationUI
    {
        private Window m_window;
        private PathPicker? m_firmwareFileOpenPicker;
        private PathPicker? m_firmwareFileSavePicker;
        private SplashDialog m_splashDialog = new();
        private OkDialog? m_okDialog;
        private Config m_config = new();
        private Settings m_settings = new();
        private bool m_firmwareLoaded = false;
        private byte[] m_firnwareData = Array.Empty<byte>();
        private bool m_showSplash = true;
        private string m_version;

        private static void DrawToggle(bool enabled, bool hovered, Vector2 pos, Vector2 size)
        {
            var drawList = ImGui.GetWindowDrawList();

            float radius = size.Y * 0.5f;
            float rounding = size.Y * 0.25f;
            float slotHalfHeight = size.Y * 0.5f;

            var background = hovered ? ImGui.GetColorU32(enabled ? ImGuiCol.FrameBgActive : ImGuiCol.FrameBgHovered) : ImGui.GetColorU32(enabled ? ImGuiCol.CheckMark : ImGuiCol.FrameBg);

            var paddingMid = new Vector2(pos.X + radius + (enabled ? 1 : 0) * (size.X - radius * 2), pos.Y + size.Y / 2);
            var sizeMin = new Vector2(pos.X, paddingMid.Y - slotHalfHeight);
            var sizeMax = new Vector2(pos.X + size.X, paddingMid.Y + slotHalfHeight);
            drawList.AddRectFilled(sizeMin, sizeMax, background, rounding);

            var offs = new Vector2(radius * 0.8f, radius * 0.8f);
            drawList.AddRectFilled(paddingMid - offs, paddingMid + offs, ImGui.GetColorU32(ImGuiCol.SliderGrab), rounding);
        }

        private static bool Toggle(string str_id, ref bool v, Vector2 size)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(new Vector4()));

            var style = ImGui.GetStyle();

            ImGui.PushID(str_id);
            bool status = ImGui.Button("###toggle_button", size);
            if (status)
            {
                v = !v;
            }
            ImGui.PopID();

            var maxRect = ImGui.GetItemRectMax();
            var toggleSize = new Vector2(size.X - 8, size.Y - 8);
            var togglePos = new Vector2(maxRect.X - toggleSize.X - style.FramePadding.X, maxRect.Y - toggleSize.Y - style.FramePadding.Y);
            DrawToggle(v, ImGui.IsItemHovered(), togglePos, toggleSize);

            ImGui.PopStyleColor();

            return status;
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, int attrSize);

        public ApplicationUI(string version)
        {
            m_window = new Window();
            m_version = version;
        }

        private Vector2 GetScaledWindowSize()
        {
            return new Vector2(m_window.Size.X, m_window.Size.Y) / m_window.Controller.GetScaleFactor();
        }

        public void OpenUrl(string url)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start("cmd", "/C start" + " " + url);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", url);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", url);
                }
            }
            catch
            {
                // do nothing
            }
        }

        public void Run()
        {
            var horizontalScale = 1.0f;
            var verticalScale = 1.0f;
            m_window.TryGetCurrentMonitorScale(out horizontalScale, out verticalScale);

            m_window.Title = $"PrometheOS Config Tool - {m_version} (Team Resurgent)";
            m_window.Size = new OpenTK.Mathematics.Vector2i((int)(510 * horizontalScale), (int)(328 * verticalScale));
            m_window.WindowBorder = OpenTK.Windowing.Common.WindowBorder.Fixed;
            m_window.VSync = OpenTK.Windowing.Common.VSyncMode.On;

            var resourceBytes = ResourceLoader.GetEmbeddedResourceBytes("PrometheOSConfigTool.Resources.icon.png");
            using var resourceImage = SixLabors.ImageSharp.Image.Load<Rgba32>(resourceBytes);
            var pixelSpan = new Span<Rgba32>(new Rgba32[resourceImage.Width * resourceImage.Height]);
            resourceImage.CopyPixelDataTo(pixelSpan);
            var byteSpan = MemoryMarshal.AsBytes(pixelSpan);
            var iconImage = new OpenTK.Windowing.Common.Input.Image(resourceImage.Width, resourceImage.Height, byteSpan.ToArray());
            m_window.Icon = new OpenTK.Windowing.Common.Input.WindowIcon(iconImage);

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000, 0))
            {
                int value = -1;
                uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
                _ = DwmSetWindowAttribute(GLFW.GetWin32Window(m_window.WindowPtr), DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
            }

            m_settings = Settings.LoadSettings();

            m_firmwareFileOpenPicker = new PathPicker
            {
                Title = "Load Firmware",
                Mode = PathPicker.PickerMode.FileOpen,
                AllowedFiles = new[] { "*.bin" },
                ButtonName = "Load"
            };

            m_firmwareFileSavePicker = new PathPicker
            {
                Title = "Save Firmware",
                Mode = PathPicker.PickerMode.FileSave,
                AllowedFiles = new[] { "*.bin" },
                SaveName = "PrometheOS.bin",
                ButtonName = "Save"
            };

            m_okDialog = new();

            m_window.RenderUI = RenderUI;
            m_window.Run();
        }

        private void RenderUI()
        {
            if (m_firmwareFileOpenPicker == null ||
                m_firmwareFileSavePicker == null ||
                m_okDialog == null)
            {
                return;
            }

            if (m_firmwareFileOpenPicker.Render() && !m_firmwareFileOpenPicker.Cancelled)
            {
                m_settings.BiosPath = m_firmwareFileOpenPicker.SelectedFolder;
                m_settings.BiosFile = m_firmwareFileOpenPicker.SelectedFile;
                m_firmwareLoaded = FirmwareUtility.LoadFirmwareComfig(Path.Combine(m_firmwareFileOpenPicker.SelectedFolder, m_firmwareFileOpenPicker.SelectedFile), ref m_config, ref m_firnwareData);
                Settings.SaveSattings(m_settings);
            }

            if (m_firmwareFileSavePicker.Render() && !m_firmwareFileSavePicker.Cancelled && string.IsNullOrEmpty(m_settings.BiosPath) == false)
            {
                var savePath = Path.Combine(m_firmwareFileSavePicker.SelectedFolder, m_firmwareFileSavePicker.SaveName);
                FirmwareUtility.SaveFirmwareConfig(m_config, Path.Combine(m_settings.BiosPath, m_settings.BiosFile), savePath, m_firnwareData, true);
            }

            m_okDialog.Render();

            m_splashDialog.Render();

            if (m_showSplash)
            {
                m_showSplash = false;
                m_splashDialog.ShowdDialog(m_window.Controller.SplashTexture);
            }

            ImGui.Begin("Main", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize);
            ImGui.SetWindowSize(GetScaledWindowSize());
            ImGui.SetWindowPos(new Vector2(0, 24), ImGuiCond.Always);

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load Firmware", "CTRL+L")) 
                    {
                        var path = Directory.Exists(m_settings.BiosPath) ? m_settings.BiosPath : Directory.GetCurrentDirectory();
                        m_firmwareFileOpenPicker.ShowModal(path);
                    }

                    if (ImGui.MenuItem("Save Firmware", "CTRL+S", false, m_firmwareLoaded))
                    {
                        var path = Directory.Exists(m_settings.BiosPath) ? m_settings.BiosPath : Directory.GetCurrentDirectory();
                        m_firmwareFileSavePicker.ShowModal(path);
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }

            ImGui.Spacing();

            ImGui.SetCursorPosY(10);
            ImGui.BeginChild(1, new Vector2(476, 212), true, ImGuiWindowFlags.AlwaysUseWindowPadding);

            if (!m_firmwareLoaded)
            {
                ImGui.BeginDisabled();
            }

            string[] avCheckValues = new string[] { "AV Check Off", "AV Check On" };
            var avCheck = (int)m_config.AVCheck;
            ImGui.Text("AV Check:");
            ImGui.PushItemWidth(460);
            ImGui.Combo("##avCheck", ref avCheck, avCheckValues, avCheckValues.Length);
            ImGui.PopItemWidth();
            m_config.AVCheck = (byte)avCheck;

            ImGui.Spacing();

            var splashDelay = (int)m_config.SplashDelay;
            ImGui.Text("Splash Delay: (0 = Do not render splash)");
            ImGui.PushItemWidth(460);
            ImGui.InputInt("##splasDelay", ref splashDelay, 0, 10, ImGuiInputTextFlags.CharsDecimal);
            ImGui.PopItemWidth();
            m_config.SplashDelay = (byte)splashDelay;

            if (!m_firmwareLoaded)
            {
                ImGui.EndDisabled();
            }

            ImGui.EndChild();

            if (!m_firmwareLoaded)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.Button("Default Config", new Vector2(100, 30)))
            {
                m_config.SetDefaults();
            }

            if (!m_firmwareLoaded)
            {
                ImGui.EndDisabled();
            }

            ImGui.SameLine();
            
            if (ImGui.Button("Patreon", new Vector2(100, 30)))
            {
                OpenUrl("https://www.patreon.com/teamresurgent");
            }

            ImGui.SameLine();

            if (ImGui.Button("Ko-Fi", new Vector2(100, 30)))
            {
                OpenUrl("https://ko-fi.com/teamresurgent");
            }

            ImGui.SameLine();

            if (ImGui.Button("Coded by EqUiNoX", new Vector2(150, 30)))
            {
                OpenUrl("https://github.com/Team-Resurgent/PrometheOSConfigTool");
            }

            ImGui.End();
        }
    }
}
