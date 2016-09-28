using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    /// <summary>
    ///     Wraps a FindFirstFile handle.
    /// </summary>
    internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SafeFindHandle" /> class.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal SafeFindHandle() : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr handle);

        /// <summary>
        ///     When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        /// <returns>
        ///     true if the handle is released successfully; otherwise, in the
        ///     event of a catastrophic failure, false. In this case, it
        ///     generates a releaseHandleFailed MDA Managed Debugging Assistant.
        /// </returns>
        protected override bool ReleaseHandle()
        {
            return FindClose(this.handle);
        }
    }
}