using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    internal class LastException
    {
        private static FieldInfo myThreadPointerFieldInfo;

        private static int myThreadOffset = -1;

        private readonly PtrConverter<Exception> myConverter = new PtrConverter<Exception>();

        public LastException()
        {
            if (myThreadPointerFieldInfo == null)
            {
                // Dont read the don´t or you will get scared. 
                // I prefer to read this more like: If you dont know the rules you will
                // get biten by the next .NET Framework update
                myThreadPointerFieldInfo = typeof(Thread).GetField("DONT_USE_InternalThread", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (myThreadOffset == -1)
            {
                var context = ClrContext.None;
                context |= (IntPtr.Size == 8) ? ClrContext.Is64Bit : ClrContext.Is32Bit;
                context |= (Environment.Version.Major == 2) ? ClrContext.IsNet2 : ClrContext.None;
                context |= (Environment.Version.Major == 4) ? ClrContext.IsNet4 : ClrContext.None;

                switch (context)
                {
                    case ClrContext.Is32Bit | ClrContext.IsNet2:
                        myThreadOffset = 0x180;
                        break;
                    case ClrContext.Is32Bit | ClrContext.IsNet4:
                        myThreadOffset = 0x188;
                        break;
                    case ClrContext.Is64Bit | ClrContext.IsNet2:
                        myThreadOffset = 0x240;
                        break;
                    case ClrContext.Is64Bit | ClrContext.IsNet4:
                        myThreadOffset = 0x250;
                        break;

                    default: // ups who did install .NET 5?
                        myThreadOffset = -1;
                        break;
                }
            }
        }

        /// <summary>
        ///     Get from the current thread the last thrown exception object.
        /// </summary>
        /// <returns>null when none exists or the exception instance.</returns>
        public Exception GetLastException()
        {
            Exception lret = null;
            if (myThreadPointerFieldInfo != null)
            {
                var pInternalThread = (IntPtr)myThreadPointerFieldInfo.GetValue(Thread.CurrentThread);
                if (pInternalThread != IntPtr.Zero && myThreadOffset != -1)
                {
                    var ppEx = Marshal.ReadIntPtr(pInternalThread, myThreadOffset);
                    if (ppEx != IntPtr.Zero)
                    {
                        var pEx = Marshal.ReadIntPtr(ppEx);
                        if (pEx != IntPtr.Zero)
                        {
                            lret = this.myConverter.ConvertFromIntPtr(pEx);
                        }
                    }
                }
            }
            return lret;
        }
    }
}