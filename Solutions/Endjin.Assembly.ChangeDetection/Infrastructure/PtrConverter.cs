using System;
using System.Reflection.Emit;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    /// <summary>
    ///     This class can convert any pointer to a managed object into a true object reference back.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PtrConverter<T>
    {
        private static Void2ObjectConverter<T> myConverter;

        // The type initializer is run every time the converter is instantiated with a different 
        // generic argument. 
        static PtrConverter()
        {
            GenerateDynamicMethod();
        }

        private static void GenerateDynamicMethod()
        {
            if (myConverter == null)
            {
                var method = new DynamicMethod("ConvertPtrToObjReference", typeof(T), new[] { typeof(IntPtr) }, StaticModule.UnsafeModule);
                var gen = method.GetILGenerator();
                // Load first argument 
                gen.Emit(OpCodes.Ldarg_0);
                // return it directly. The Clr will take care of the cast!
                // this construct is unverifiable so we need to plug this into an assembly with 
                // IL Verification disabled
                gen.Emit(OpCodes.Ret);
                myConverter = (Void2ObjectConverter<T>)method.CreateDelegate(typeof(Void2ObjectConverter<T>));
            }
        }

        /// <summary>
        ///     Convert a pointer to a managed object back to the original object reference
        /// </summary>
        /// <param name="pObj">Pointer to managed object</param>
        /// <returns>Object reference</returns>
        /// <exception cref="ExecutionEngineException">
        ///     When the pointer does not point to valid CLR object. This can happen when the GC decides to move object references
        ///     to new memory locations.
        ///     Beware this possibility exists all the time (although the probability should be very low)!
        /// </exception>
        public T ConvertFromIntPtr(IntPtr pObj)
        {
            return myConverter(pObj);
        }

        private delegate U Void2ObjectConverter<U>(IntPtr pManagedObject);
    }
}