using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

class iloveherguyssss
{
    private static Timer? _timer;
    private static int _tableTop;
    private static int _lastLineCount;
    private static readonly ConcurrentDictionary<int, byte> _knownPids = new();
    private static Mutex? _mutex;
    private static readonly nint _currentProcess = Process.GetCurrentProcess().Handle;

    private const uint STATUS_SUCCESS = 0;
    private const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_DUP_HANDLE = 0x0040;
    private const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
    private const uint DUPLICATE_SAME_ACCESS = 0x00000002;
    private const int MAX_CLIENTS = 9999;

    private enum PROCESSINFOCLASS { ProcessHandleInformation = 51 }
    private enum OBJECT_INFORMATION_CLASS { ObjectNameInformation = 1 }

    [StructLayout(LayoutKind.Sequential)]
    private struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public nint Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OBJECT_NAME_INFORMATION { public UNICODE_STRING Name; }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_HANDLE_TABLE_ENTRY_INFO
    {
        public nint HandleValue;
        public nuint HandleCount;
        public nuint PointerCount;
        public uint GrantedAccess;
        public uint ObjectTypeIndex;
        public uint HandleAttributes;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_HANDLE_SNAPSHOT_INFORMATION
    {
        public nuint NumberOfHandles;
        public nuint Reserved;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(nint hSourceProcess, nint hSourceHandle,
        nint hTargetProcess, out nint lpTargetHandle, uint dwDesiredAccess,
        bool bInheritHandle, uint dwOptions);

    [DllImport("ntdll.dll")]
    private static extern uint NtQueryInformationProcess(nint ProcessHandle, PROCESSINFOCLASS ProcessInformationClass,
        nint ProcessInformation, uint ProcessInformationLength, out uint ReturnLength);

    [DllImport("ntdll.dll")]
    private static extern uint NtQueryObject(nint Handle, OBJECT_INFORMATION_CLASS ObjectInformationClass,
        nint ObjectInformation, uint ObjectInformationLength, out uint ReturnLength);

    static void Main()
    {
        Console.Title = "RobloxOmegaAlpha.sh";
        Console.Clear();
        Console.WriteLine("This tool is against Roblox's ToS.\nUse it at your own risk.\n");

        using var cts = new CancellationTokenSource();
        var waitTask = Task.Delay(45000, cts.Token);
        var keyTask = Task.Run(() => { while (Console.ReadKey(true).Key != ConsoleKey.Enter) { } });
        Task.WhenAny(waitTask, keyTask).Wait();
        cts.Cancel();

        try
        {
            _mutex = new Mutex(true, "ROBLOX_singletonMutex");
        }
        
        catch
        {
            return;
        }

        Console.Clear();
        _tableTop = Console.CursorTop;
        _timer = new Timer(UpdateDisplay, null, 0, 1000);
        Thread.Sleep(Timeout.Infinite);
    }

    static void UpdateDisplay(object? state)
    {
        lock (Console.Out)
        {
            var procs = Process.GetProcessesByName("RobloxPlayerBeta").OrderBy(p => p.Id).ToArray();
            bool showMessage = false;

            foreach (var p in procs)
            {
                if (_knownPids.ContainsKey(p.Id)) continue;
                try
                {
                    if (_knownPids.Count >= MAX_CLIENTS)
                    {
                        p.Kill();
                        showMessage = true;
                    }
                    else
                    {
                        CloseRemoteMutex(p.Id);
                        _knownPids.TryAdd(p.Id, 0);
                    }
                }
                catch { }
            }

            var active = Process.GetProcessesByName("RobloxPlayerBeta")
                .Where(p => _knownPids.ContainsKey(p.Id))
                .OrderBy(p => p.Id)
                .ToArray();

            Console.SetCursorPosition(0, _tableTop);
            ClearLines(_lastLineCount);
            Console.SetCursorPosition(0, _tableTop);

            int lines;
            if (active.Length == 0)
            {
                Console.Clear();
                Console.WriteLine("RobloxPlayerBeta.exe not found.");
                Console.WriteLine("Is yours roblox even running? Btw, ts might not work with roblox from microslop store");
                lines = 2;
            }
            
            else
            {
                for (int i = 0; i < active.Length; i++)
                    Console.WriteLine($"{i + 1}: PID = {active[i].Id}");
                    lines = active.Length;

                if (showMessage)
                {
                    Console.WriteLine("You noob");
                    lines++;
                }
            }

            if (lines < _lastLineCount)
            {
                Console.SetCursorPosition(0, _tableTop + lines);
                ClearLines(_lastLineCount - lines);
            }

            _lastLineCount = lines;
        }
    }

    static void ClearLines(int count)
    {
        int top = Console.CursorTop;
        string blank = new(' ', Console.WindowWidth);
        for (int i = 0; i < count; i++)
        {
            Console.SetCursorPosition(0, top + i);
            Console.Write(blank);
        }
        Console.SetCursorPosition(0, top);
    }

    private static void CloseRemoteMutex(int pid)
    {
        nint hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_DUP_HANDLE, false, pid);
        if (hProcess == 0) return;

        try
        {
            NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessHandleInformation, 0, 0, out uint bufSize);
            if (bufSize == 0) return;

            nint buffer = Marshal.AllocHGlobal((int)bufSize);
            try
            {
                if (NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessHandleInformation, buffer, bufSize, out _) != STATUS_SUCCESS)
                    return;

                var snap = Marshal.PtrToStructure<PROCESS_HANDLE_SNAPSHOT_INFORMATION>(buffer);
                int entrySize = Marshal.SizeOf<PROCESS_HANDLE_TABLE_ENTRY_INFO>();
                nint handlesPtr = buffer + Marshal.SizeOf<PROCESS_HANDLE_SNAPSHOT_INFORMATION>();

                for (int i = 0; i < (int)snap.NumberOfHandles; i++)
                {
                    var entry = Marshal.PtrToStructure<PROCESS_HANDLE_TABLE_ENTRY_INFO>(handlesPtr + i * entrySize);
                    if (!DuplicateHandle(hProcess, entry.HandleValue, _currentProcess, out nint dupHandle, 0, false, DUPLICATE_SAME_ACCESS))
                        continue;

                    try
                    {
                        string? name = GetObjectName(dupHandle);
                        if (name != null && name.EndsWith("\\ROBLOX_singletonMutex"))
                        {
                            DuplicateHandle(hProcess, entry.HandleValue, 0, out _, 0, false, DUPLICATE_CLOSE_SOURCE);
                            break;
                        }
                    }
                    finally
                    {
                        CloseHandle(dupHandle);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    private static string? GetObjectName(nint handle)
    {
        string? result = null;
        var task = Task.Run(() =>
        {
            NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, 0, 0, out uint nameLen);
            if (nameLen == 0) return;

            nint nameBuffer = Marshal.AllocHGlobal((int)nameLen);
            try
            {
                if (NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, nameBuffer, nameLen, out _) != STATUS_SUCCESS)
                    return;

                var info = Marshal.PtrToStructure<OBJECT_NAME_INFORMATION>(nameBuffer);
                result = Marshal.PtrToStringUni(info.Name.Buffer, info.Name.Length / 2);
            }
            finally
            {
                Marshal.FreeHGlobal(nameBuffer);
            }
        });

        task.Wait(50);
        return result;
    }
}
