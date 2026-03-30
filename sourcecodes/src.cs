using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

class skibiditoilet69
{
    private static Timer? _timer;
    private static int _tableTop;
    private static int lastLineCount = 0;
    private static HashSet<int> knownPids = new HashSet<int>();
    private static Mutex? _mutex;
    private const uint STATUS_SUCCESS = 0;
    private const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_DUP_HANDLE = 0x0040;
    private const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
    private const uint DUPLICATE_SAME_ACCESS = 0x00000002;
    private const int MAX_CLIENTS = 9999; // set to 5 if you want only 5 clients running or higher/lower //

    public enum PROCESSINFOCLASS { ProcessHandleInformation = 51 }
    public enum OBJECT_INFORMATION_CLASS { ObjectNameInformation = 1 }

    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_NAME_INFORMATION
    {
        public UNICODE_STRING Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_HANDLE_TABLE_ENTRY_INFO
    {
        public IntPtr HandleValue;
        public UIntPtr HandleCount;
        public UIntPtr PointerCount;
        public uint GrantedAccess;
        public uint ObjectTypeIndex;
        public uint HandleAttributes;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_HANDLE_SNAPSHOT_INFORMATION
    {
        public UIntPtr NumberOfHandles;
        public UIntPtr Reserved;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, 
        IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, 
        bool bInheritHandle, uint dwOptions);

    [DllImport("ntdll.dll")]
    private static extern uint NtQueryInformationProcess(IntPtr ProcessHandle, PROCESSINFOCLASS ProcessInformationClass, 
        IntPtr ProcessInformation, uint ProcessInformationLength, out uint ReturnLength);

    [DllImport("ntdll.dll")]
    private static extern uint NtQueryObject(IntPtr Handle, OBJECT_INFORMATION_CLASS ObjectInformationClass, 
        IntPtr ObjectInformation, uint ObjectInformationLength, out uint ReturnLength);

    static void Main(string[] args)
    {
        Console.Title = "RobloxMultiAlpha.rat";
        Console.Clear();
        Console.WriteLine("This tool is AGAINIST Roblox's TOS!");
        Console.WriteLine("Use at your risk nga.");
        Console.WriteLine(" ");
        var delayTask = Task.Delay(45000); // Stops for 45 seconds. Can be skipped //
        var keyTask = Task.Run(() =>
        {
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
            }
            
            while (key.Key != ConsoleKey.Enter);
            
        });
        
        Task.WhenAny(delayTask, keyTask).Wait();

        _tableTop = Console.CursorTop;
        try
            
        {
            _mutex = new Mutex(true, "ROBLOX_singletonMutex");
        }
        
        catch
        {
            return
        }

        _timer = new Timer(UpdateDisplay, null, 0, 1000);
        while (true)
        {
            Thread.Sleep(1000);
        }
    // catch something //
        
    }

    static void UpdateDisplay(object? state)
    {
        lock (Console.Out)
        {
            var procs = Process.GetProcessesByName("RobloxPlayerBeta").OrderBy(p => p.Id).ToArray();
            Console.SetCursorPosition(0, _tableTop);
            ClearBelow(lastLineCount);
            Console.SetCursorPosition(0, _tableTop);

            bool showMessage = false;

            foreach (var p in procs)
            {
                if (!knownPids.Contains(p.Id))
                {
                    try
                    {
                        if (knownPids.Count >= MAX_CLIENTS)
                        {
                            p.Kill();
                            showMessage = true;
                        }
                        else
                        {
                            CloseRemoteMutex(p.Id);
                            knownPids.Add(p.Id);
                        }
                    }
                    catch {}
                }
            }

            var displayProcs = Process.GetProcessesByName("RobloxPlayerBeta")
                .Where(p => knownPids.Contains(p.Id))
                .OrderBy(p => p.Id)
                .ToArray();

            int newLineCount = 0;
            if (displayProcs.Length == 0)
            {
                Console.Clear();
                Console.WriteLine("RobloxPlayerBeta.exe Not Found. Make sure Roblox is running!!!");
                newLineCount = 1;
            }
            else
            {
                for (int i = 0; i < displayProcs.Length; i++)
                {
                    Console.WriteLine($"{i + 1}: PID = {displayProcs[i].Id}");
                }
                
                newLineCount = displayProcs.Length;

                if (showMessage)
                {
                    Console.WriteLine("Uh, Hi ig.");
                    Console.WriteLine("");
                    newLineCount += 2;
                }
            }

            if (newLineCount < lastLineCount)
            {
                Console.SetCursorPosition(0, _tableTop + newLineCount);
                ClearBelow(lastLineCount - newLineCount);
            }
            lastLineCount = newLineCount;
        }
    }

    static void ClearBelow(int lines)
    {
        for (int i = 0; i < lines; i++)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.CursorTop++;
        }
    }

    private static void CloseRemoteMutex(int pid)
    {
        IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_DUP_HANDLE, false, pid);
        if (hProcess == IntPtr.Zero) return;

        try
        {
            uint returnLength = 0;
            uint status = NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessHandleInformation, IntPtr.Zero, 0, out returnLength);
            if (status != STATUS_INFO_LENGTH_MISMATCH) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)returnLength);
            try
            {
                status = NtQueryInformationProcess(hProcess, PROCESSINFOCLASS.ProcessHandleInformation, buffer, returnLength, out returnLength);
                if (status != STATUS_SUCCESS) return;

                var snap = Marshal.PtrToStructure<PROCESS_HANDLE_SNAPSHOT_INFORMATION>(buffer);
                int entrySize = Marshal.SizeOf<PROCESS_HANDLE_TABLE_ENTRY_INFO>();
                IntPtr handlesPtr = buffer + Marshal.SizeOf<PROCESS_HANDLE_SNAPSHOT_INFORMATION>();

                for (int i = 0; i < (int)snap.NumberOfHandles; i++)
                {
                    var entry = Marshal.PtrToStructure<PROCESS_HANDLE_TABLE_ENTRY_INFO>(handlesPtr + i * entrySize);

                    IntPtr dupHandle;
                    if (!DuplicateHandle(hProcess, entry.HandleValue, Process.GetCurrentProcess().Handle, out dupHandle, 0, false, DUPLICATE_SAME_ACCESS)) continue;

                    try
                    {
                        uint nameLen = 0;
                        NtQueryObject(dupHandle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, IntPtr.Zero, 0, out nameLen);
                        if (nameLen == 0) continue;

                        IntPtr nameBuffer = Marshal.AllocHGlobal((int)nameLen);
                        try
                        {
                            status = NtQueryObject(dupHandle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, nameBuffer, nameLen, out nameLen);
                            if (status != STATUS_SUCCESS) continue;

                            var nameInfo = Marshal.PtrToStructure<OBJECT_NAME_INFORMATION>(nameBuffer);
                            string? objName = Marshal.PtrToStringUni(nameInfo.Name.Buffer, nameInfo.Name.Length / 2);

                            if (objName != null && objName.EndsWith("\\ROBLOX_singletonMutex"))
                            {
                                DuplicateHandle(hProcess, entry.HandleValue, IntPtr.Zero, out _, 0, false, DUPLICATE_CLOSE_SOURCE);
                                break;
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(nameBuffer);
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
}
