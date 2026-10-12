using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EmuWPF.ECores
{
    /// <summary>
    /// Catches an emulator's main window the moment Windows creates it,
    /// makes it fully transparent so it never flashes on screen, and hands
    /// back the handle so it can be embedded. Call Reveal() once embedded.
    ///
    /// Must be created on the UI thread (it relies on the WPF message pump).
    /// </summary>
    public sealed class WindowCatcher : IDisposable
    {
        // ── Win32 ────────────────────────────────────────────────
        const uint EVENT_OBJECT_CREATE = 0x8000;
        const uint EVENT_OBJECT_SHOW = 0x8002;
        const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
        const int OBJID_WINDOW = 0;
        const int GWL_EXSTYLE = -20;
        const long WS_EX_LAYERED = 0x00080000L;
        const long WS_EX_TOOLWINDOW = 0x00000080L;
        const uint LWA_ALPHA = 0x2;
        const uint GA_ROOT = 2;
        const uint RDW_INVALIDATE = 0x0001;
        const uint RDW_ALLCHILDREN = 0x0080;
        const uint RDW_UPDATENOW = 0x0100;

        delegate void WinEventProc(IntPtr hHook, uint evt, IntPtr hwnd,
                                   int idObject, int idChild, uint thread, uint time);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmod,
                                             WinEventProc proc, uint pid, uint tid, uint flags);
        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
        [DllImport("user32.dll")]
        static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr hwnd, StringBuilder sb, int max);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint key, byte alpha, uint flags);
        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hwnd, IntPtr rect, IntPtr rgn, uint flags);

        // ── State ────────────────────────────────────────────────
        readonly WinEventProc _proc;   // kept as a field so the GC can't collect it
        readonly IntPtr _hook;
        readonly TaskCompletionSource<IntPtr> _found =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        uint _pid;

        public WindowCatcher()
        {
            _proc = OnWinEvent;
            // Hook before the process starts so the window's creation can't be missed.
            _hook = SetWinEventHook(EVENT_OBJECT_CREATE, EVENT_OBJECT_SHOW, IntPtr.Zero,
                                    _proc, 0, 0,
                                    WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
        }

        /// <summary>Call straight after Process.Start.</summary>
        public void Watch(Process process) => _pid = (uint)process.Id;

        /// <summary>
        /// Waits for the emulator's main window. Returns IntPtr.Zero if the
        /// process exits or the timeout passes.
        /// </summary>
        public async Task<IntPtr> WaitAsync(Process process, int timeoutMs = 10000)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var done = await Task.WhenAny(_found.Task, Task.Delay(50));
                if (done == _found.Task) return _found.Task.Result;

                if (process.HasExited) return IntPtr.Zero;

                // Fallback in case the show event was missed
                process.Refresh();
                var main = process.MainWindowHandle;
                if (main != IntPtr.Zero && IsCandidate(main))
                {
                    MakeInvisible(main);
                    return main;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Makes the window visible again. Call after SetParent + MoveWindow,
        /// so it appears already in place inside the app.
        /// </summary>
        public static void Reveal(IntPtr hwnd)
        {
            long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex & ~WS_EX_LAYERED));
            RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero,
                         RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW);
        }

        // ── Internals ────────────────────────────────────────────
        void OnWinEvent(IntPtr hHook, uint evt, IntPtr hwnd,
                        int idObject, int idChild, uint thread, uint time)
        {
            if (_pid == 0 || hwnd == IntPtr.Zero) return;
            if (idObject != OBJID_WINDOW || idChild != 0) return;

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid != _pid) return;
            if (GetAncestor(hwnd, GA_ROOT) != hwnd) return;   // top-level windows only
            if (!IsCandidate(hwnd)) return;

            // Hide as early as possible; the show event confirms it's the real window.
            MakeInvisible(hwnd);

            if (evt == EVENT_OBJECT_SHOW)
                _found.TrySetResult(hwnd);
        }

        static bool IsCandidate(IntPtr hwnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hwnd, sb, sb.Capacity);
            string cls = sb.ToString();

            // Skip dialogs (error boxes must stay visible), IME and tooltip windows
            if (cls == "#32770" || cls == "IME" || cls == "MSCTFIME UI" ||
                cls == "tooltips_class32")
                return false;

            long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            return (ex & WS_EX_TOOLWINDOW) == 0;
        }

        static void MakeInvisible(IntPtr hwnd)
        {
            long ex = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            if ((ex & WS_EX_LAYERED) == 0)
                SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex | WS_EX_LAYERED));
            SetLayeredWindowAttributes(hwnd, 0, 0, LWA_ALPHA);
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero) UnhookWinEvent(_hook);
        }
    }
}