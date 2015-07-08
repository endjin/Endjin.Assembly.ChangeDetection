namespace AssemblyDifferences.Infrastructure
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Security.Permissions;

    internal class StaticModule
    {
        private const string ModuleAssemblyName = "AloisDynamicCaster";

        private static Module myUnsafeModule;

        public static Module UnsafeModule
        {
            get
            {
                if (myUnsafeModule == null)
                {
                    var assemblyName = new AssemblyName(ModuleAssemblyName);
                    var aBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                    var mBuilder = aBuilder.DefineDynamicModule(ModuleAssemblyName);
                    // set SkipVerification=true on our assembly to prevent VerificationExceptions which warn
                    // about unsafe things but we want to do unsafe things after all.
                    var secAttrib = typeof(SecurityPermissionAttribute);
                    var secCtor = secAttrib.GetConstructor(new[] { typeof(SecurityAction) });
                    var attribBuilder = new CustomAttributeBuilder(secCtor, new object[] { SecurityAction.Assert }, new[] { secAttrib.GetProperty("SkipVerification", BindingFlags.Instance | BindingFlags.Public) }, new object[] { true });

                    aBuilder.SetCustomAttribute(attribBuilder);
                    var tb = mBuilder.DefineType("MyDynamicType", TypeAttributes.Public);
                    myUnsafeModule = tb.CreateType().Module;
                }

                return myUnsafeModule;
            }
        }
    }
}