namespace AssemblyDifferences.Infrastructure
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Threading;

    /// <summary>
    ///     Use this class in your catch(Exception ex) handler to determine if you can safely swallow and trace
    ///     this exception or if it is so severe that it should be propagated to the highest layer. It is mandatory to use
    ///     this function inside your finalizer to guard the finalizer thread against exceptions.
    /// </summary>
    public static class ExceptionHelper
    {
        private static readonly LastException myExceptionGetter = new LastException();

        /// <summary>
        ///     Check if a exception unwind in happening right now. This check is normally needed in a
        ///     a finally clause to check.
        /// </summary>
        /// <example>
        ///     Take different actions in a finally block depending if an exception was thrown or not.
        ///     <code>
        /// using ApiChange.Infrastructure;
        /// 
        /// void Func()
        /// {
        ///     try
        ///     {
        ///         DoSomethingThatMightThrowExceptions();
        ///     }
        ///     finally
        ///     {
        ///         if( ExceptionHelper.InException ) 
        ///         {  // When an exception happened do only cleanup and do not wait for completion.
        ///            CloseConnection();   
        ///         }
        ///         else
        ///         {
        ///            // Might not work if the network connection is lost and the call will never return
        ///            CallServerAndWaitForCompletion(); 
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        ///     More information about the technical details can be found at
        ///     http://geekswithblogs.net/akraus1/archive/2008/04/08/121121.aspx
        /// </remarks>
        public static bool InException
        {
            get
            {
                return ((Marshal.GetExceptionPointers() == IntPtr.Zero && Marshal.GetExceptionCode() == 0) ? false : true);
            }
        }

        /// <summary>
        ///     Return the current exception when the stack is unwound due to a
        ///     thrown exception. This does work everywhere and not only in catch methods.
        /// </summary>
        public static Exception CurrentException
        {
            get
            {
                Exception lret = null;
                if (InException)
                {
                    lret = myExceptionGetter.GetLastException();
                }

                return lret;
            }
        }

        /// <summary>
        ///     It is mandatory to use this function inside your finalizer to guard the finalizer thread against exceptions.
        ///     Determines whether the specified exception is fatal and should be propagated to the next layer regardless of
        ///     your current exception policy.
        ///     An exception is considered fatal is it is one of the following types:
        ///     NullReferenceException, StackOverflowException, OutOfMemoryException, ThreadAbortException,
        ///     ExecutionEngineException, IndexOutOfRangeException or AccessViolationException
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns>
        ///     <c>true</c> if the specified exception is fatal; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFatal(Exception ex)
        {
            if (!(ex is NullReferenceException) && !(ex is StackOverflowException) && !(ex is OutOfMemoryException) && !(ex is ThreadAbortException) && !(ex is IndexOutOfRangeException) && !(ex is AccessViolationException))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Determines whether the exception is a security or fatal exception. Use this functions in
        ///     security relevant code inside any catch(Exception ex) handler to prevent swallowing relevant
        ///     exceptions.
        /// </summary>
        /// <param name="ex">The exception to test.</param>
        /// <returns>
        ///     <c>true</c> if [is security or fatal] [the specified ex]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSecurityOrFatal(Exception ex)
        {
            if (!(ex is SecurityException))
            {
                return IsFatal(ex);
            }
            return true;
        }

        /// <summary>
        ///     Normally used in a finally block to execute code when an exception has occured.
        /// </summary>
        /// <param name="executeWhenExceptionHasOccured">The method to execute when exception has occured.</param>
        /// <example>
        ///     <code>
        /// using ApiChange.Infrastructure;
        /// 
        /// void Func()
        /// {
        ///     try
        ///     {
        ///         DoSomethingThatMightThrowExceptions();
        ///     }
        ///     finally
        ///     {
        ///         ExceptionHelper.WhenException( () => Console.WriteLine("Something bad happened inside this method"));
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void WhenException(Action executeWhenExceptionHasOccured)
        {
            // be robust and prevent masking the original error
            if (executeWhenExceptionHasOccured == null)
            {
                return;
            }

            if (InException)
            {
                executeWhenExceptionHasOccured();
            }
        }
    }
}