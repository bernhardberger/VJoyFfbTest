using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using vJoyInterfaceWrap;

namespace VJoyFfbTest
{
    class ForceFeedbackManager
    {
        private Callback _callback;
        public delegate void Callback(IntPtr data, IntPtr userData);


        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void FfbRegisterGenCB (Callback cb, IntPtr data);

        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static bool FfbStart(uint rId);

        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static void FfbStop(uint rId);


        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static bool Ffb_h_DeviceID(IntPtr data, ref int id);

        [DllImport("vJoyInterface.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static bool Ffb_h_Type(IntPtr data, ref int type);

        public ForceFeedbackManager()
        {
            _callback = new Callback(OnEffect);
        }

        public void Run()
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
                throw new Exception(String.Format(error, index));

            joystick.ResetVJD(index);


            if (!FfbStart(index))
                throw new Exception(String.Format("Failed to start Forcefeedback on device {0}", index));

            FfbRegisterGenCB(_callback, IntPtr.Zero);

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

            Debug.WriteLine("Device: {0} - Effect type: {1}", id, type);

            //Will crash when this method returns
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            var ffbManager = new ForceFeedbackManager();
            ffbManager.Run();
        }
    }
}
