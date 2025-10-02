// ===============================================
// Diagnostics/Logger.cs
// Thin wrapper on plugin log; warning badge for conflicts
// ===============================================

using ExileCore2;

namespace WellCrafted.Diagnostics
{
    public static class Logger
    {
        public static void Debug(string message)
        {
            DebugWindow.LogMsg($"[WellCrafted] {message}");
        }

        public static void Info(string message)
        {
            DebugWindow.LogMsg($"[WellCrafted] {message}");
        }

        public static void Warning(string message)
        {
            DebugWindow.LogMsg($"[WellCrafted] WARNING: {message}");
        }

        public static void Error(string message)
        {
            DebugWindow.LogError($"[WellCrafted] ERROR: {message}");
        }
    }
}