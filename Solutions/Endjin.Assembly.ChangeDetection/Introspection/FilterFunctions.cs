namespace AssemblyDifferences.Introspection
{
    using System;

    using Mono.Cecil;

    public static class FilterFunctions
    {
        public static Func<TypeDefinition, MethodDefinition, bool> ApiMethodFilter
        {
            get
            {
                return (TypeDefinition typeDef, MethodDefinition methodDef) =>
                {
                    var lret = false;

                    return lret;
                };
            }
        }

        private static bool IsEnabled(FilterMode mode, FilterMode requestedFilter)
        {
            return ((mode & requestedFilter) == requestedFilter);
        }

        /// <summary>
        ///     Generate an event filter function with the required semantics
        /// </summary>
        /// <param name="mode">Filter for event visibility</param>
        /// <returns>Comparison function</returns>
        public static Func<TypeDefinition, EventDefinition, bool> GetEventFilter(FilterMode mode)
        {
            Func<TypeDefinition, EventDefinition, bool> func = (TypeDefinition typeDef, EventDefinition evDef) =>
            {
                var lret = false;

                if (IsEnabled(mode, FilterMode.Public))
                {
                    lret = evDef.AddMethod.IsPublic;
                }
                if (!lret && IsEnabled(mode, FilterMode.Protected))
                {
                    if (evDef.AddMethod.IsAssembly && IsEnabled(mode, FilterMode.NotInternalProtected))
                    {
                        // skip internal events which could be protected
                    }
                    else
                    {
                        lret = evDef.AddMethod.IsFamily;
                    }
                }
                if (!lret && IsEnabled(mode, FilterMode.Private))
                {
                    lret = evDef.AddMethod.IsPrivate;
                }
                if (!lret && IsEnabled(mode, FilterMode.Internal))
                {
                    lret = evDef.AddMethod.IsAssembly;
                }

                return lret;
            };

            return func;
        }

        public static Func<TypeDefinition, FieldDefinition, bool> GetFieldFilter(FilterMode mode)
        {
            Func<TypeDefinition, FieldDefinition, bool> func = (TypeDefinition typeDef, FieldDefinition fieldDef) =>
            {
                var lret = false;

                if (IsEnabled(mode, FilterMode.Public))
                {
                    lret = fieldDef.IsPublic;
                }
                if (!lret && IsEnabled(mode, FilterMode.Protected))
                {
                    if (fieldDef.IsAssembly && IsEnabled(mode, FilterMode.NotInternalProtected))
                    {
                        // skip internal fields which could be protected
                    }
                    else
                    {
                        lret = fieldDef.IsFamily && !fieldDef.IsAssembly;
                    }
                }
                if (!lret && IsEnabled(mode, FilterMode.Private))
                {
                    lret = fieldDef.IsPrivate;
                }
                if (!lret && IsEnabled(mode, FilterMode.Internal))
                {
                    lret = fieldDef.IsAssembly;
                }

                // skip event fields in class we already have the event definition in place
                if (lret)
                {
                    foreach (EventDefinition ev in typeDef.Events)
                    {
                        if (ev.Name == fieldDef.Name)
                        {
                            return false;
                        }
                    }
                }

                return lret;
            };

            return func;
        }

        public static Func<TypeDefinition, MethodDefinition, bool> GetMethodFilter(FilterMode mode)
        {
            Func<TypeDefinition, MethodDefinition, bool> func = (TypeDefinition typeDef, MethodDefinition methodDef) =>
            {
                var lret = false;

                if (IsEnabled(mode, FilterMode.Public))
                {
                    lret = methodDef.IsPublic;
                }
                if (!lret && IsEnabled(mode, FilterMode.Protected))
                {
                    if (methodDef.IsAssembly && IsEnabled(mode, FilterMode.NotInternalProtected))
                    {
                        // skip internal fields which could be protected
                    }
                    else
                    {
                        lret = methodDef.IsFamily && !methodDef.IsAssembly;
                    }
                }
                if (!lret && IsEnabled(mode, FilterMode.Private))
                {
                    lret = methodDef.IsPrivate;
                }
                if (!lret && IsEnabled(mode, FilterMode.Internal))
                {
                    lret = methodDef.IsAssembly;
                }

                // Skip event add remove functions from normal methods
                // in class
                if (lret)
                {
                    foreach (EventDefinition ev in typeDef.Events)
                    {
                        if (ev.AddMethod.IsEqual(methodDef) || ev.RemoveMethod.IsEqual(methodDef))
                        {
                            lret = false;
                            break;
                        }
                    }
                }

                return lret;
            };

            return func;
        }
    }
}