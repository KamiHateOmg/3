using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;

namespace PremiumLoader
{
    public class LoadingService
    {
        private static readonly List<HookFunction> _functions = new List<HookFunction>()
        {
            new HookFunction("LoadLibraryExW", "kernel32"),
            new HookFunction("VirtualAlloc", "kernel32"),
            new HookFunction("FreeLibrary", "kernel32"),
            new HookFunction("LoadLibraryExA", "kernel32"),
            new HookFunction("LoadLibraryW", "kernel32"),
            new HookFunction("LoadLibraryA", "kernel32"),
            new HookFunction("VirtualAllocEx", "kernel32"),
            new HookFunction("LdrLoadDll", "ntdll"),
            new HookFunction("NtOpenFile", "ntdll"),
            new HookFunction("VirtualProtect", "kernel32"),
            new HookFunction("CreateProcessW", "kernel32"),
            new HookFunction("CreateProcessA", "kernel32"),
            new HookFunction("VirtualProtectEx", "kernel32"),
            new HookFunction("FreeLibrary", "KernelBase"),
            new HookFunction("LoadLibraryExA", "KernelBase"),
            new HookFunction("LoadLibraryExW", "KernelBase"),
            new HookFunction("ResumeThread", "KernelBase")
        };

        private byte[,]? _originalBytes;
        private IntPtr _targetHandle = IntPtr.Zero;
        private uint _targetProcessId = uint.MinValue;
        private readonly object _lockObject = new object();

        public bool LoadDll(string dllPath)
        {
            lock (_lockObject)
            {
                try
                {
                    Initialize();
                    
                    if (!ValidateDll(dllPath))
                        throw new FileNotFoundException($"Invalid or missing DLL: {dllPath}");

                    _targetProcessId = LocateTargetProcess();
                    if (_targetProcessId == uint.MinValue)
                        throw new InvalidOperationException("CS2 process not found. Ensure the game is running.");

                    _targetHandle = OpenTargetProcess();
                    if (_targetHandle == IntPtr.Zero)
                        throw new UnauthorizedAccessException("Cannot access CS2 process. Run as administrator.");

                    // Execute loading sequence
                    PerformSecurityBypass();
                    ExecuteLoading(dllPath);
                    RestoreSecurityHooks();

                    return true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Loading failed: {ex.Message}", ex);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        private void Initialize()
        {
            _originalBytes = new byte[_functions.Count, 6];
            _targetHandle = IntPtr.Zero;
            _targetProcessId = uint.MinValue;
        }

        private bool ValidateDll(string dllPath)
        {
            if (string.IsNullOrWhiteSpace(dllPath) || !File.Exists(dllPath))
                return false;

            var fileInfo = new FileInfo(dllPath);
            return fileInfo.Length > 0 && fileInfo.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase);
        }

        private uint LocateTargetProcess()
        {
            // CS2 process names to search for (prioritize cs2)
            string[] processNames = { "cs2", "Counter-Strike 2" };
            
            foreach (string processName in processNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName)
                        .Where(p => !p.HasExited)
                        .ToArray();

                    foreach (var process in processes)
                    {
                        // Additional validation for CS2
                        if (IsValidCS2Process(process))
                        {
                            if (process.MainWindowHandle != IntPtr.Zero)
                            {
                                GetWindowThreadProcessId(process.MainWindowHandle, out uint pid);
                                return pid > 0 ? pid : (uint)process.Id;
                            }
                            return (uint)process.Id;
                        }
                    }
                }
                catch (Exception)
                {
                    // Continue searching with next process name
                    continue;
                }
            }

            return uint.MinValue;
        }

        private bool IsValidCS2Process(Process process)
        {
            try
            {
                // Validate process characteristics
                if (process.ProcessName.Contains("cs2", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Check process path/title for CS2 indicators
                string windowTitle = process.MainWindowTitle?.ToLower() ?? "";
                return windowTitle.Contains("counter-strike") || windowTitle.Contains("cs2");
            }
            catch
            {
                return false;
            }
        }

        private IntPtr OpenTargetProcess()
        {
            IntPtr handle = OpenProcess(ProcessAccessFlags.All, false, (int)_targetProcessId);
            
            if (handle == IntPtr.Zero)
            {
                // Try with reduced permissions
                handle = OpenProcess(
                    ProcessAccessFlags.VirtualMemoryOperation | 
                    ProcessAccessFlags.VirtualMemoryWrite | 
                    ProcessAccessFlags.VirtualMemoryRead | 
                    ProcessAccessFlags.CreateThread, 
                    false, (int)_targetProcessId);
            }

            return handle;
        }

        private void ExecuteLoading(string dllPath)
        {
            if (!File.Exists(dllPath))
                throw new FileNotFoundException($"DLL not found: {dllPath}");

            // Allocate memory in target process
            byte[] pathBytes = Encoding.UTF8.GetBytes(dllPath);
            IntPtr allocatedMemory = VirtualAllocEx(
                _targetHandle, 
                IntPtr.Zero, 
                (IntPtr)pathBytes.Length, 
                AllocationType.Reserve | AllocationType.Commit,
                MemoryProtection.ExecuteReadWrite);

            if (allocatedMemory == IntPtr.Zero)
                throw new InvalidOperationException("Memory allocation failed in target process.");

            try
            {
                // Write DLL path to allocated memory
                if (!WriteProcessMemory(_targetHandle, allocatedMemory, pathBytes, pathBytes.Length, out _))
                    throw new InvalidOperationException("Failed to write DLL path to target process.");

                // Get LoadLibraryA address
                IntPtr kernel32 = GetModuleHandle("kernel32.dll");
                IntPtr loadLibraryAddr = GetProcAddress(kernel32, "LoadLibraryA");

                if (loadLibraryAddr == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to locate LoadLibraryA function.");

                // Create and execute remote thread
                IntPtr threadHandle = CreateRemoteThread(
                    _targetHandle,
                    IntPtr.Zero,
                    0,
                    loadLibraryAddr,
                    allocatedMemory,
                    0,
                    IntPtr.Zero);

                if (threadHandle == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to create remote thread for loading.");

                // Wait for thread completion with timeout
                uint waitResult = WaitForSingleObject(threadHandle, 5000); // 5 second timeout
                
                if (waitResult != 0) // WAIT_OBJECT_0
                    throw new TimeoutException("Loading thread did not complete within timeout period.");

                CloseHandle(threadHandle);
            }
            finally
            {
                // Clean up allocated memory
                VirtualFreeEx(_targetHandle, allocatedMemory, 0, AllocationType.Release);
            }
        }

        private void PerformSecurityBypass()
        {
            for (int i = 0; i < _functions.Count; i++)
            {
                try
                {
                    UnhookFunction(_functions[i].Name, _functions[i].Module, i);
                }
                catch
                {
                    // Continue with other functions if one fails
                    continue;
                }
            }
        }

        private void RestoreSecurityHooks()
        {
            for (int i = 0; i < _functions.Count; i++)
            {
                try
                {
                    RestoreFunction(_functions[i].Name, _functions[i].Module, i);
                }
                catch
                {
                    // Continue with other functions if one fails
                    continue;
                }
            }
        }

        private bool UnhookFunction(string functionName, string moduleName, int index)
        {
            if (_originalBytes == null) return false;
            
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero)
                return false;

            IntPtr functionAddress = GetProcAddress(moduleHandle, functionName);
            if (functionAddress == IntPtr.Zero)
                return false;

            byte[] originalBytes = new byte[6];
            if (!ReadProcessMemory(_targetHandle, functionAddress, originalBytes, 6, out _))
                return false;

            // Store original bytes
            for (int i = 0; i < originalBytes.Length; i++)
            {
                _originalBytes[index, i] = originalBytes[i];
            }

            // Get clean function bytes from current process
            byte[] cleanBytes = new byte[6];
            GCHandle pinnedArray = GCHandle.Alloc(cleanBytes, GCHandleType.Pinned);
            try
            {
                IntPtr cleanBytesPtr = pinnedArray.AddrOfPinnedObject();
                memcpy(cleanBytesPtr, functionAddress, (UIntPtr)6);
                
                return WriteProcessMemory(_targetHandle, functionAddress, cleanBytes, 6, out _);
            }
            finally
            {
                pinnedArray.Free();
            }
        }

        private bool RestoreFunction(string functionName, string moduleName, int index)
        {
            if (_originalBytes == null) return false;
            
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero)
                return false;

            IntPtr functionAddress = GetProcAddress(moduleHandle, functionName);
            if (functionAddress == IntPtr.Zero)
                return false;

            byte[] originalBytes = new byte[6];
            for (int i = 0; i < originalBytes.Length; i++)
            {
                originalBytes[i] = _originalBytes[index, i];
            }

            return WriteProcessMemory(_targetHandle, functionAddress, originalBytes, 6, out _);
        }

        private void Cleanup()
        {
            if (_targetHandle != IntPtr.Zero)
            {
                CloseHandle(_targetHandle);
                _targetHandle = IntPtr.Zero;
            }
        }

        private class HookFunction
        {
            public string Name { get; }
            public string Module { get; }

            public HookFunction(string name, string module)
            {
                Name = name;
                Module = module;
            }
        }

        #region Win32 API Declarations

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, IntPtr dwSize, 
            AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize,
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, 
            int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, 
            int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        #endregion
    }
}