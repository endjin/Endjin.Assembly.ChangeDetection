namespace AssemblyDifferences.Introspection
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using AssemblyDifferences.Diff;

    using Mono.Cecil;
    using Mono.Collections.Generic;

    public static class TypeExtensions
    {
        public static TypeDiff GetTypeByName(this HashSet<TypeDiff> set, string typeName)
        {
            TypeDiff lret = null;
            foreach (TypeDiff type in set)
            {
                if (String.CompareOrdinal(type.ToString(), typeName) == 0)
                {
                    lret = type;
                    break;
                }
            }

            return lret;
        }

        public static FieldDefinition GetFieldByeName(this List<FieldDefinition> list, string fieldName)
        {
            return GetFieldByNameAndType(list, fieldName, null);
        }

        public static FieldDefinition GetFieldByNameAndType(this List<FieldDefinition> list, string fieldName, string fieldType)
        {
            FieldDefinition lret = null;

            foreach (var field in list)
            {
                if (String.CompareOrdinal(field.Name, fieldName) == 0)
                {
                    if (!String.IsNullOrEmpty(fieldType))
                    {
                        if (field.FieldType != null &&
                            field.FieldType.FullName == fieldType)
                        {
                            lret = field;
                            break;
                        }
                    }
                    else
                    {
                        lret = field;
                        break;
                    }
                }
            }

            return lret;
        }


        public static EventDefinition GetEventByName(this List<EventDefinition> list, string evName)
        {
            return GetEventByNameAndType(list, evName, null);
        }

        public static EventDefinition GetEventByNameAndType(this List<EventDefinition> list, string evName, string type)
        {
            EventDefinition lret = null;

            foreach (var ev in list)
            {
                if (ev.Name == evName)
                {
                    if (type == null)
                    {
                        lret = ev;
                    }
                    else
                    {
                        lret = (ev.EventType.FullName == type) ? ev : null;
                    }

                    break;
                }
            }

            return lret;
        }


        static void AddVisibility(StringBuilder sb, bool isPublic, bool isPrivate, bool isFamily, bool isAssembly, bool isProtectedInternal)
        {
            if (isPublic)
                sb.Append("public ");
            if (isPrivate)
                sb.Append("private ");
            if (isFamily)
                sb.Append("protected ");
            if (isAssembly)
                sb.Append("internal ");
            if (isProtectedInternal)
                sb.Append("protected internal ");
        }

        public static string Print(this EventDefinition ev)
        {
            StringBuilder sb = new StringBuilder();

            MethodDefinition evMethod = ev.AddMethod;

            AddVisibility(sb, evMethod.IsPublic, evMethod.IsPrivate, evMethod.IsFamily, evMethod.IsAssembly, evMethod.IsFamilyOrAssembly);

            if (evMethod.IsVirtual)
                sb.AppendFormat("{0} ", "virtual");
            if (evMethod.IsStatic)
                sb.AppendFormat("{0} ", "static");

            sb.Append("event ");

            string typeName;
            if (ev.EventType is GenericInstanceType)
            {
                GenericInstanceType genFieldType = (GenericInstanceType)ev.EventType;
                StringBuilder typeSb = new StringBuilder();
                sb.Append(genFieldType.Name.Split(new char[] { '`' })[0]);
                PrintGenericArgumentCollection(typeSb, genFieldType.GenericArguments);
                typeName = typeSb.ToString();
            }
            else
            {
                typeName = ev.EventType.FullName;
                typeName = TypeMapper.FullToShort(typeName);
            }
            sb.Append(typeName);

            sb.Append(" ");
            sb.Append(ev.Name);

            return sb.ToString();
        }

        public static string Print(this FieldDefinition field, FieldPrintOptions options)
        {
            StringBuilder sb = new StringBuilder();

            if ((options & FieldPrintOptions.Visibility) == FieldPrintOptions.Visibility)
            {
                AddVisibility(sb, field.IsPublic, field.IsPrivate, field.IsFamily, field.IsAssembly, field.IsFamilyOrAssembly);
            }

            if ((options & FieldPrintOptions.Modifiers) == FieldPrintOptions.Modifiers)
            {
                if (field.IsStatic && !field.HasConstant)
                {
                    sb.Append("static ");
                }

                if (field.IsInitOnly)
                {
                    sb.Append("readonly ");
                }

                if (field.HasConstant)
                {
                    sb.Append("const ");
                }
            }

            string typeName = "";
            if ((options & FieldPrintOptions.SimpleType) == FieldPrintOptions.SimpleType)
            {
                GenericInstanceType genType = field.FieldType as GenericInstanceType;
                if (genType != null)
                {
                    typeName = genType.Print();
                }
                else
                {
                    typeName = field.FieldType.FullName;
                    typeName = TypeMapper.FullToShort(typeName);
                }
            }
            sb.Append(typeName);


            sb.AppendFormat(" {0}", field.Name);

            if (((options & FieldPrintOptions.Value) == FieldPrintOptions.Value) &&
                field.HasConstant)
            {
                sb.AppendFormat(" - Value: {0}", field.Constant);
            }

            return sb.ToString();
        }

        static char[] myGenericParamSep = new char[] { '`' };

        static string RemoveGenericParameterCountFromName(string name)
        {
            return name.Split(myGenericParamSep)[0];
        }


        public static string Print(this GenericInstanceType type)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(RemoveGenericParameterCountFromName(type.Name));
            PrintGenericArgumentCollection(sb, type.GenericArguments);
            return sb.ToString();
        }

        public static string Print(this TypeDefinition type)
        {
            StringBuilder sb = new StringBuilder();
            if (type.IsPublic)
            {
                sb.Append("public ");
            }
            else
            {
                sb.Append("internal ");
            }
            if (type.IsInterface)
            {
                sb.Append("interface ");
            }
            else if (type.IsEnum)
            {
                sb.Append("enum ");
            }
            else if (type.IsValueType)
            {
                sb.Append("struct ");
            }
            else
            {
                sb.Append("class ");
            }

            if (type.DeclaringType != null)
            {
                sb.AppendFormat("{0}/", type.DeclaringType.FullName);
            }
            else
            {
                sb.Append(type.Namespace);
                sb.Append(".");
            }

            sb.Append(type.Name.Split(new char[] { '`' })[0]);
            PrintGenericParameterCollection(sb, type.GenericParameters);
            return sb.ToString();
        }

        /// <summary>
        /// A type reference to a generic contains the concrete types to fully declare it.
        /// Like List&lt;string&gt; where string is an argument to the generic type.
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="coll"></param>
        static void PrintGenericArgumentCollection(StringBuilder sb, Collection<TypeReference> coll)
        {
            if (coll == null || coll.Count == 0)
                return;

            sb.Append("<");
            for (int i = 0; i < coll.Count; i++)
            {
                TypeReference param = coll[i];
                GenericInstanceType generic = param as GenericInstanceType;
                if (generic != null)
                {
                    sb.Append(RemoveGenericParameterCountFromName(generic.Name));
                    PrintGenericArgumentCollection(sb, generic.GenericArguments);
                }
                else
                {
                    sb.Append(TypeMapper.FullToShort(param.Name));

                    // Print recursive definition
                    if (i != coll.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
            }
            sb.Append(">");

        }

        /// <summary>
        /// A type definition can contain generic parameters which are printed out here
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="coll"></param>
        static void PrintGenericParameterCollection(StringBuilder sb, Collection<GenericParameter> coll)
        {
            if (coll == null || coll.Count == 0)
            {
                return;
            }

            sb.Append("<");
            for (int i = 0; i < coll.Count; i++)
            {
                GenericParameter param = coll[i];
                sb.Append(TypeMapper.FullToShort(param.Name));
                // Print recursive definition
                PrintGenericParameterCollection(sb, param.GenericParameters);
                if (i != coll.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append(">");
        }


        public static string Print(this MethodReference method, MethodPrintOption options)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2}",
                method.DeclaringType.FullName,
                method.ReturnType.Name,
                method.Name);
            PrintParameters(method.Parameters, sb, options);
            return sb.ToString();
        }

        static void PrintParameters(Collection<ParameterDefinition> parameters, StringBuilder sb, MethodPrintOption options)
        {
            bool bPrintNames = ((options & MethodPrintOption.ParamNames) == MethodPrintOption.ParamNames);
            sb.Append("(");
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterDefinition parameter = parameters[i];

                if (parameter.IsOut)
                    sb.Append("out ");

                string paramType = null;

                GenericInstanceType generic = parameter.ParameterType as GenericInstanceType;
                if (generic != null)
                    paramType = generic.Print();

                if (paramType == null)
                {
                    paramType = parameter.ParameterType.Name;
                }

                // Ref types seem not to be correctly parsed by mono cecil so we leave the & syntax
                // inside it for the time beeing.
                if (parameter.IsOut)
                {
                    paramType = paramType.TrimEnd(new char[] { '&' });
                }

                sb.Append(paramType);
                if (bPrintNames)
                {
                    sb.AppendFormat(" {0}", parameter.Name);
                }
                // parameter.Name
                if (i != parameters.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append(")");
        }

        public static string Print(this MethodDefinition method, MethodPrintOption options)
        {
            StringBuilder sb = new StringBuilder();
            bool bPrintAlias = ((MethodPrintOption.ShortNames & MethodPrintOption.ShortNames) == MethodPrintOption.ShortNames);

            if ((MethodPrintOption.Visiblity & options) == MethodPrintOption.Visiblity)
            {
                AddVisibility(sb, method.IsPublic, method.IsPrivate, method.IsFamily, method.IsAssembly, method.IsFamilyOrAssembly);
            }

            if ((MethodPrintOption.Modifier & options) == MethodPrintOption.Modifier)
            {
                if (method.IsVirtual && method.HasBody)
                    sb.AppendFormat("{0} ", "virtual");
                if (method.IsStatic)
                    sb.AppendFormat("{0} ", "static");
            }


            if ((MethodPrintOption.ReturnType & options) == MethodPrintOption.ReturnType)
            {
                string retType = null;
                if (bPrintAlias)
                {
                    GenericInstanceType generic = method.ReturnType as GenericInstanceType;
                    if (generic != null)
                        retType = generic.Print();
                }
                if (retType == null)
                    retType = TypeMapper.FullToShort(method.ReturnType.FullName);

                sb.AppendFormat("{0} ", retType);
            }

            sb.Append(method.Name);

            if ((options & MethodPrintOption.Parameters) == MethodPrintOption.Parameters)
            {
                PrintParameters(method.Parameters, sb, options);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Compares two TypeReferences by its Full Name and declaring assembly
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>
        /// 	<c>true</c> if the specified x is equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEqual(this TypeDefinition x, TypeDefinition y)
        {
            if (x == null && y == null)
                return true;

            if ((x == null && y != null) ||
                (x != null && y == null))
            {
                return false;
            }

            return x.FullName == y.FullName &&
                   x.Scope.IsEqual(y.Scope);
        }

        static bool Contains(this Collection<TypeReference> interfaces, TypeReference itfTypeRef)
        {
            foreach (TypeReference typeRef in interfaces)
            {
                if (typeRef.IsEqual(itfTypeRef))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if two methods are equal with respect to name, return type, visibility and method modifiers.
        /// </summary>
        /// <param name="m1">The m1.</param>
        /// <param name="m2">The m2.</param>
        /// <returns></returns>
        public static bool IsEqual(this MethodDefinition m1, MethodDefinition m2)
        {
            bool lret = false;


            // check if function name, modifiers and paramters are still equal
            if (m1 != null &&
                  m1.Name == m2.Name &&
                  m1.ReturnType.FullName == m2.ReturnType.FullName &&
                  m1.Parameters.Count == m2.Parameters.Count &&
                  m1.IsPrivate == m2.IsPrivate &&
                  m1.IsPublic == m2.IsPublic &&
                  m1.IsFamily == m2.IsFamily &&
                  m1.IsAssembly == m2.IsAssembly &&
                  m1.IsFamilyOrAssembly == m2.IsFamilyOrAssembly &&
                  m1.IsVirtual == m2.IsVirtual &&
                  m1.IsStatic == m2.IsStatic &&
                  m1.GenericParameters.Count == m2.GenericParameters.Count
             )
            {
                bool bParameterEqual = true;

                // Check function parameter types if there has been any change
                for (int i = 0; i < m1.Parameters.Count; i++)
                {
                    ParameterDefinition pa = m1.Parameters[i];
                    ParameterDefinition pb = m2.Parameters[i];

                    if (pa.ParameterType.FullName != pb.ParameterType.FullName)
                    {
                        bParameterEqual = false;
                    }
                }

                lret = bParameterEqual;
            }

            return lret;
        }

        public static bool IsEqual(this EventDefinition e1, EventDefinition e2)
        {
            bool lret = false;

            if (
                e1.Name == e2.Name &&
                e1.EventType.FullName == e2.EventType.FullName &&
                e1.AddMethod.IsEqual(e2.AddMethod))
            {
                lret = true;
            }

            return lret;
        }


        public static bool IsEqual(this MethodReference x, MethodReference y)
        {
            return x.IsEqual(y, true);
        }

        public static bool IsEqual(this MethodReference x, MethodReference y, bool bCompareGenericParameters)
        {
            if (x == null && y == null)
                return true;

            if ((x == null && y != null) ||
                (x != null && y == null))
            {
                return false;
            }

            bool lret = false;

            if (x.Name == y.Name &&
                x.DeclaringType.GetElementType().IsEqual(y.DeclaringType.GetElementType(), bCompareGenericParameters) &&
                x.ReturnType.IsEqual(y.ReturnType, bCompareGenericParameters) &&
                x.Parameters.IsEqual(y.Parameters, bCompareGenericParameters))
            {
                if (bCompareGenericParameters)
                    lret = x.GenericParameters.IsEqual(y.GenericParameters);
                else
                    lret = true;
            }

            return lret;
        }

        public static bool IsEqual(this Collection<ParameterDefinition> x, Collection<ParameterDefinition> y)
        {
            return x.IsEqual(y, true);
        }

        public static bool IsEqual(this Collection<ParameterDefinition> x, Collection<ParameterDefinition> y, bool bCompareGenericParameters)
        {
            if (x == null && y == null)
                return true;

            if ((x == null && y != null) ||
                (x != null && y == null))
            {
                return false;
            }

            if (x.Count != y.Count)
                return false;

            for (int i = 0; i < x.Count; i++)
            {
                ParameterDefinition p1 = x[i];
                ParameterDefinition p2 = y[i];


                if (!p1.ParameterType.IsEqual(p2.ParameterType, bCompareGenericParameters))
                {
                    return false;
                }

                // There seems to be a bug in mono cecil. MethodReferences do not 
                // contain the IsIn/IsOut property data we would need to check if both methods
                // have the same In/Out signature for this parameter.
                if (p1.MetadataToken.RID == p2.MetadataToken.RID)
                {
                    if ((p1.IsIn != p2.IsIn) || (p1.IsOut != p2.IsOut))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static string ExractAssemblyNameFromScope(IMetadataScope x)
        {
            AssemblyNameReference aRef = x as AssemblyNameReference;
            if (aRef != null)
                return aRef.Name;

            AssemblyNameDefinition aDef = x as AssemblyNameDefinition;
            if (aDef != null)
                return aDef.Name;

            ModuleDefinition aMod = x as ModuleDefinition;
            if (aMod != null)
                return aMod.Assembly.Name.Name;

            // normally the module name has the dll name but 
            // this is not the case for mscorlib where CommonLanguageRuntime is inside it
            ModuleReference aModRef = x as ModuleReference;
            if (aModRef != null)
                return Path.GetFileNameWithoutExtension(aModRef.Name);

            return x.Name;
        }

        public static bool IsEqual(this IMetadataScope x, IMetadataScope y)
        {
            if (x == null && y == null)
                return true;

            if ((x == null && y != null) ||
              (x != null && y == null))
            {
                return false;
            }

            string xName = ExractAssemblyNameFromScope(x);
            string yName = ExractAssemblyNameFromScope(y);

            return xName == yName;
        }

        public static bool IsEqual(this TypeReference x, TypeReference y)
        {
            bool lret = x.IsEqual(y, true);
            return lret;
        }

        public static bool IsEqual(this GenericInstanceType x, GenericInstanceType y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if ((x == null && y != null) ||
                (x != null && y == null))
            {
                return false;
            }

            return x.GenericArguments.IsEqual(y.GenericArguments);
        }

        /// <summary>
        /// Type names for generic in class instantiations beginn with ! to denote the number of
        /// the generic argument of the enclosing class.
        /// System.Collections.Generic.KeyValuePair`2&lt;int32,float32&gt;::.ctor(!0,!1)
        /// where !0 and !1 are the position number of the generic type.
        /// Method references to generic functions specify the type reference by two !! to number them
        /// e.g. !!0 !!1
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        static bool AreTypeNamesEqual(string n1, string n2)
        {
            if (n1 == n2)
                return true;

            if (n1[0] == '!' ||
                n2[0] == '!')
                return true;

            return false;
        }

        public static void IsNotNull(this object value, Action acc)
        {
            if (value != null)
            {
                acc();
            }
        }

        public static T IsNotNull<T>(this object value, Func<T> acc) where T : class
        {
            if (value != null)
            {
                return acc();
            }
            return null;
        }

        public static bool IsEqual(this TypeReference x, TypeReference y, bool bCompareGenericParameters)
        {
            if (x == null && y == null)
                return true;

            if ((x == null && y != null) ||
                (x != null && y == null))
            {
                return false;
            }

            bool lret = false;

            TypeReference xDeclaring = x.DeclaringType;
            TypeReference yDeclaring = y.DeclaringType;

            xDeclaring.IsNotNull(() => xDeclaring = x.DeclaringType.GetElementType());
            yDeclaring.IsNotNull(() => yDeclaring = y.DeclaringType.GetElementType());


            // Generic parameters are passed as placeholder via method reference
            //  newobj instance void class [BaseLibraryV1]BaseLibrary.ApiChanges.PublicGenericClass`1<string>::.ctor(class [System.Core]System.Func`1<!0>)
            if (AreTypeNamesEqual(x.Name, y.Name) &&
                x.Namespace == y.Namespace &&
                IsEqual(xDeclaring, yDeclaring, bCompareGenericParameters) &&
                x.Scope.IsEqual(y.Scope))
            {
                if (bCompareGenericParameters)
                {
                    lret = x.GenericParameters.IsEqual(y.GenericParameters);
                }
                else
                {
                    lret = true;
                }
            }

            if (lret)
            {
                // Generics can have 
                GenericInstanceType xGen = x as GenericInstanceType;
                GenericInstanceType yGen = y as GenericInstanceType;
                if (xGen != null && yGen != null)
                {
                    lret = xGen.IsEqual(yGen);
                }
            }

            return lret;
        }

        public static bool IsEqual(this FieldDefinition f1, FieldDefinition f2)
        {
            bool lret = false;

            if (f1 != null &&
                f1.IsPublic == f2.IsPublic &&
                f1.IsFamilyOrAssembly == f2.IsFamilyOrAssembly &&
                f1.IsFamily == f2.IsFamily &&
                f1.IsAssembly == f2.IsAssembly &&
                f1.IsPrivate == f2.IsPrivate &&
                f1.IsStatic == f2.IsStatic &&
                f1.HasConstant == f2.HasConstant &&
                f1.IsInitOnly == f2.IsInitOnly &&
                f1.Name == f2.Name &&
                f1.FieldType.FullName == f2.FieldType.FullName)
            {
                lret = true;
            }

            return lret;
        }

        public static List<FieldDefinition> FilterFields(this TypeDefinition type, FilterMode mode)
        {
            var filter = FilterFunctions.GetFieldFilter(mode);
            List<FieldDefinition> fields = new List<FieldDefinition>();
            foreach (FieldDefinition fieldDef in type.Fields)
            {
                if (filter(type, fieldDef))
                    fields.Add(fieldDef);
            }

            return fields;
        }

        public static List<MethodDefinition> FilterMethods(this TypeDefinition type, FilterMode mode)
        {
            var filter = FilterFunctions.GetMethodFilter(mode);
            List<MethodDefinition> methods = new List<MethodDefinition>();
            foreach (MethodDefinition methodDef in type.Methods)
            {
                if (filter(type, methodDef))
                    methods.Add(methodDef);
            }
            return methods;
        }

        public static List<EventDefinition> FilterEvents(this TypeDefinition type, FilterMode mode)
        {
            var filter = FilterFunctions.GetEventFilter(mode);
            List<EventDefinition> events = new List<EventDefinition>();
            foreach (EventDefinition ev in type.Events)
            {
                if (filter(type, ev))
                    events.Add(ev);
            }

            return events;
        }

        public static bool IsEqual(this Collection<TypeReference> genArgs1, Collection<TypeReference> genArgs2)
        {
            if (genArgs1 == null && genArgs2 == null)
            {
                return true;
            }

            if ((genArgs1 == null && genArgs2 != null) ||
                 (genArgs1 != null && genArgs2 == null))
            {
                return false;
            }

            if (genArgs1.Count != genArgs2.Count)
                return false;

            for (int i = 0; i < genArgs1.Count; i++)
            {
                TypeReference type1 = genArgs1[i];
                TypeReference type2 = genArgs2[i];
                if (!type1.IsEqual(type2))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsEqual(this Collection<GenericParameter> collection1, Collection<GenericParameter> collection2)
        {
            if (collection1 == null && collection2 == null)
                return true;

            if ((collection1 == null && collection2 != null) ||
                (collection1 != null && collection2 == null))
                return false;

            if (collection1.Count != collection2.Count)
                return false;

            bool lret = true;

            for (int i = 0; i < collection1.Count; i++)
            {
                GenericParameter param1 = collection1[i];
                GenericParameter param2 = collection2[i];

                if (param1.FullName != param2.FullName && param1.FullName[0] != '!' && param2.FullName[0] != '!')
                {
                    lret = false;
                    break;
                }
            }

            return lret;
        }
    }
}