using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using vJoyInterfaceWrap;

namespace VJoyFfbTest
{
    class Program
    {
        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static bool FfbStart(uint rId);

        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static void FfbStop(uint rId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void Callback(IntPtr data, IntPtr userData);

        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static void FfbRegisterGenCB(Callback cb, IntPtr data);

        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static bool Ffb_h_DeviceID(IntPtr data, ref int id);

        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static bool Ffb_h_Type(IntPtr data, ref int type);
        
        static void Main(string[] args)
        {

            uint index = 1;
            var joystick = new vJoy();

            if (!joystick.vJoyEnabled())
                throw new Exception("vJoy driver not enabled: Failed Getting vJoy attributes");
            

            var status = joystick.GetVJDStatus(index);

            string error = null;
            switch (status)
            {
                case VjdStat.VJD_STAT_BUSY:
                    error = "vJoy Device {0} is already owned by another feeder";
                    break;
                case VjdStat.VJD_STAT_MISS:
                    error = "vJoy Device {0} is not installed or disabled";
                    break;
                case VjdStat.VJD_STAT_UNKN:
                    error = ("vJoy Device {0} general error");
                    break;
            }

            if (error == null && !joystick.AcquireVJD(index))
                error = "Failed to acquire vJoy device number {0}";

            if (error != null)
                throw new Exception(string.Format(error, index));

            joystick.ResetVJD(index);


            if (!FfbStart(index))
                throw new Exception(string.Format("Failed to start Forcefeedback on device {0}", index));

            FfbRegisterGenCB(OnEffect, IntPtr.Zero);

            Console.ReadLine();

            FfbStop(index);
            joystick.RelinquishVJD(index);   
        }

        private static void OnEffect(IntPtr data, IntPtr ptr)
        {
            int id = 0;
            int type = 0;

            //var msg = (FFB_DATA)Marshal.PtrToStructure(data, typeof(FFB_DATA));

            var result = Ffb_h_DeviceID(data, ref id);
            result = Ffb_h_Type(data, ref type); //Does this work, returns 17 for all effect types

            System.Diagnostics.Debug.WriteLine("Device: {0} - Effect type: {1}", id, type);

            //Will crash when this method returns
        }
    }
}
