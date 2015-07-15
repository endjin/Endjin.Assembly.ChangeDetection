namespace Endjin.SemanticVersioning.TeamCity
{
    #region Using Directives

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    #endregion

    public static class AssemblyAttributeExtensions
    {
        public static T GetAssemblyAttribute<T>(this Assembly assembly) where T : Attribute
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(T), false);

            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes.OfType<T>().SingleOrDefault();
        }

        public static IEnumerable<Attribute> GetAssemblyAttributes(this Assembly assembly)
        {
            var attribues = assembly.GetCustomAttributes(false);

            return from attribute in attribues where attribute.GetType() != typeof(AssemblyConfigurationAttribute) && 
                                                     attribute.GetType() != typeof(AssemblyFileVersionAttribute) &&
                                                     attribute.GetType() != typeof(AssemblyInformationalVersionAttribute) &&
                                                     attribute.GetType() != typeof(AssemblyVersionAttribute) select attribute as Attribute;
        }
    }
}