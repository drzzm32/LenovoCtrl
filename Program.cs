using System;
using System.IO;
using System.Runtime.InteropServices;

namespace LenovoCtrl
{
    class CtrlCore : IDisposable
    {
        public IntPtr acpiVpcDriverHandle;
        public const string acpiVpcDriverName = @"\\.\EnergyDrv";

        [StructLayout(LayoutKind.Sequential)]
        public struct FanCtrl
        {
            public uint ControlType;
            public uint DataCount;
            public uint Data;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile([MarshalAs(UnmanagedType.LPTStr)] string filename, [MarshalAs(UnmanagedType.U4)] FileAccess access, [MarshalAs(UnmanagedType.U4)] FileShare share, IntPtr securityAttributes, [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition, [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes, IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

        public CtrlCore()
        {
            acpiVpcDriverHandle = IntPtr.Zero;
            acpiVpcDriverHandle = CreateFile(acpiVpcDriverName, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            if (acpiVpcDriverHandle.ToInt32() == -1)
            {
                object[] args = new object[] { acpiVpcDriverName, Marshal.GetLastWin32Error() };
                Console.WriteLine("Get handler to device {0} failed with error code {1} ", args);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (((acpiVpcDriverHandle != IntPtr.Zero) && (acpiVpcDriverHandle.ToInt32() != -1)) && (disposing && CloseHandle(acpiVpcDriverHandle)))
            {
                acpiVpcDriverHandle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool DoDeviceIoControl(uint uIoControlCode, object input, ref object output)
        {
            bool flag = false;
            IntPtr zero = IntPtr.Zero;
            IntPtr ptr = IntPtr.Zero;
            int cb = 0;
            int num2 = 0;
            if (input != null)
            {
                cb = Marshal.SizeOf(input);
                zero = Marshal.AllocCoTaskMem(cb);
                Marshal.StructureToPtr(input, zero, false);
            }
            if (output != null)
            {
                num2 = Marshal.SizeOf(output);
                ptr = Marshal.AllocCoTaskMem(num2);
                Marshal.StructureToPtr(output, ptr, false);
            }
            if ((acpiVpcDriverHandle != IntPtr.Zero) && (acpiVpcDriverHandle.ToInt32() != -1))
            {
                uint lpBytesReturned = 0;
                flag = DeviceIoControl(acpiVpcDriverHandle, uIoControlCode, zero, (uint)cb, ptr, (uint)num2, out lpBytesReturned, IntPtr.Zero);
                if (flag)
                {
                    if ((ptr != IntPtr.Zero) && (output != null))
                    {
                        output = Marshal.PtrToStructure(ptr, output.GetType());
                    }
                }
                else
                {
                    object[] args = new object[] { acpiVpcDriverName, Marshal.GetLastWin32Error(), acpiVpcDriverHandle, uIoControlCode, zero, (uint)cb, ptr, (uint)num2, lpBytesReturned, IntPtr.Zero };
                    Console.WriteLine(@"DeviceIoControl failed with error code {0}\n\t hDevice={1}\n\t dwIoControlCode={2}\n\t lpInBuffer={3}\n\t nInBufferSize={4}\n\t lpOutBuffer={5}\n\t nOutBufferSize={6}\n\t lpBytesReturned={7}\n\t lpOverlapped={8}", args);
                }
                if (zero != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(zero);
                }
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(ptr);
                }
            }
            return flag;
        }

        public int GetBacklightStatus()
        {
            uint num = 0;
            object input = 2;
            object output = num;
            if (DoDeviceIoControl(0x831020e8, input, ref output))
            {
                return (int)(uint)output; //0x10 -> one-state, 0x20 -> on
            }
            object[] args = new object[] { Marshal.GetLastWin32Error() };
            Console.WriteLine("DeviceIoControl failed with error code {0} ", args);
            return -1;
        }

        public void SetBacklightStatus(int value)
        {
            uint num = (uint)(value == 1 ? 8 : 9); //9 -> off, 8 -> on
            object input; 

            input = num;
            object output = 0;
            if (!DoDeviceIoControl(0x831020e8, input, ref output))
            {
                object[] args = new object[] { Marshal.GetLastWin32Error() };
                Console.WriteLine("DeviceIoControl failed with error code {0} ", args);
            }
        }

        public void DoFanDustRemoval()
        {
            FanCtrl ctrl = new FanCtrl
            {
                ControlType = 6,
                DataCount = 1,
                Data = 1
            };
            object input = ctrl;
            object output = null;
            if (DoDeviceIoControl(0x831020c0, input, ref output))
            {
                Console.WriteLine("DoFanDustRemoval applied");
                Console.WriteLine("Status: {0}", GetFanDustRemovalStatus());
                return;
            }
            object[] args = new object[] { Marshal.GetLastWin32Error() };
            Console.WriteLine("DeviceIoControl failed with error code {0} ", args);
        }

        public void CancelFanDustRemoval()
        {
            FanCtrl ctrl = new FanCtrl
            {
                ControlType = 6,
                DataCount = 1,
                Data = 0
            };
            object input = ctrl;
            object output = null;
            if (DoDeviceIoControl(0x831020c0, input, ref output))
            {
                Console.WriteLine("CancelFanDustRemoval applied");
                Console.WriteLine("Status: {0}", GetFanDustRemovalStatus());
                return;
            }
            object[] args = new object[] { Marshal.GetLastWin32Error() };
            Console.WriteLine("DeviceIoControl failed with error code {0} ", args);
        }

        public string GetFanDustRemovalStatus()
        {
            uint num = 0;
            object input = 14;
            object output = num;
            if (this.DoDeviceIoControl(0x831020c4, input, ref output))
            {
                num = (uint)output;
                object[] objArray1 = new object[] { num };
                //Console.WriteLine("GetDustRemovalStatus retrieved value 0x{0:X} by using IOCTL_ENERGYDRV_INFO", objArray1);
                if ((num & 1) == 1)
                {
                    if ((num & 2) == 2)
                    {
                        return "Running";
                    }
                    
                    if ((num & 4) == 4)
                    {
                        if ((num & 0x80) == 0x80)
                        {
                            return "Canceled";
                        }
                        return "Completed";
                    }
                    return "Not started";
                }
                return "No capability";
            }
            object[] args = new object[] { Marshal.GetLastWin32Error() };
            Console.WriteLine("DeviceIoControl failed with error code {0} ", args);
            return "Null";
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            CtrlCore core = new CtrlCore();
            int value = -1;

            if (args.Length == 1)
            {
                switch (args[0].ToLower())
                {
                    case "l":
                        Console.WriteLine("KbdBkCtrl v0.01\n");
                        Console.WriteLine("Current state is: {0:X00}", core.GetBacklightStatus());
                        Console.Write("Input state: ");
                        value = int.Parse(Console.ReadLine());
                        core.SetBacklightStatus(value);
                        Console.WriteLine("OK! Now state is: {0:X00}", core.GetBacklightStatus());
                        break;
                    case "f":
                        Console.WriteLine("FanCtrl v0.01\n");
                        Console.WriteLine("Current state is: {0}", core.GetFanDustRemovalStatus());
                        Console.Write("Input state: ");
                        value = int.Parse(Console.ReadLine());
                        if (value == 1) core.DoFanDustRemoval();
                        else core.CancelFanDustRemoval();
                        Console.WriteLine("OK! Now state is: {0}", core.GetFanDustRemovalStatus());
                        break;
                }
            }
            else
            {
                Console.WriteLine("LenovoCtrl v0.01\n");
                Console.WriteLine("Use arg: l, f");
            }
            Console.ReadKey(true);

            core.Dispose();
        }
    }
}
