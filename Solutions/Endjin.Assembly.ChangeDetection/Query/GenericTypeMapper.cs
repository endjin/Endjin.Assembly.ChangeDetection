using System;
using System.Collections.Generic;
using System.Text;
using Endjin.Assembly.ChangeDetection.Introspection;

namespace Endjin.Assembly.ChangeDetection.Query
{
    internal static class GenericTypeMapper
    {
        public static string TransformGenericTypeNames(string typeName, Func<string, string> typeNameTransformer)
        {
            if (typeNameTransformer == null)
            {
                throw new ArgumentNullException("typeNameTransformer");
            }

            if (string.IsNullOrEmpty(typeName))
            {
                return typeName;
            }

            var normalizedName = typeName.Replace(" ", "");

            var formattedType = normalizedName;

            var root = ParseGenericType(normalizedName);
            if (root != null)
            {
                TransformGeneric(root, typeNameTransformer);

                var sb = new StringBuilder();
                FormatExpandedGeneric(sb, root);
                formattedType = sb.ToString();
            }
            return formattedType;
        }

        private static void TransformGeneric(GenericType type, Func<string, string> typeNameTransformer)
        {
            if (type == null)
            {
                return;
            }

            type.GenericTypeName = typeNameTransformer(type.GenericTypeName);
            foreach (var typeArg in type.Arguments)
            {
                TransformGeneric(typeArg, typeNameTransformer);
            }
        }

        public static string ConvertClrTypeNames(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return typeName;
            }

            var normalizedName = typeName.Replace(" ", "");

            // No generic type then we need no mapping
            if (typeName.IndexOf('<') == -1)
            {
                return TypeMapper.ShortToFull(typeName);
            }

            var root = ParseGenericType(normalizedName);

            var sb = new StringBuilder();
            FormatExpandedGeneric(sb, root);
            var formattedType = sb.ToString();

            return formattedType;
        }

        private static GenericType ParseGenericType(string normalizedName)
        {
            var curArg = new StringBuilder();
            GenericType root = null;
            GenericType curType = null;

            // Func< Func<Func<int,int>,bool> >
            // Func`1< Func`2< Func`2<System.Int32,System.Int32>, System.Boolean> >
            // Func<int,bool,int>
            for (var i = 0; i < normalizedName.Length; i++)
            {
                if (normalizedName[i] == '<')
                {
                    if (curType == null)
                    {
                        curType = new GenericType(curArg.ToString(), null);
                        root = curType;
                    }
                    else
                    {
                        var newGeneric = new GenericType(curArg.ToString(), curType);
                        curType.Arguments.Add(newGeneric);
                        curType = newGeneric;
                    }
                    curArg.Length = 0;
                }
                else if (normalizedName[i] == '>')
                {
                    if (curArg.Length > 0)
                    {
                        curType.Arguments.Add(new GenericType(TypeMapper.ShortToFull(curArg.ToString()), null));
                    }

                    if (curType.Parent != null)
                    {
                        curType = curType.Parent;
                    }
                    curArg.Length = 0;
                }
                else if (normalizedName[i] == ',')
                {
                    if (curArg.Length > 0)
                    {
                        curType.Arguments.Add(new GenericType(TypeMapper.ShortToFull(curArg.ToString()), null));
                    }
                    curArg.Length = 0;
                }
                else
                {
                    curArg.Append(normalizedName[i]);
                }
            }
            return root;
        }

        private static void FormatExpandedGeneric(StringBuilder sb, GenericType type)
        {
            sb.Append(type.GenericTypeName);
            if (type.Arguments.Count > 0)
            {
                sb.AppendFormat("`{0}", type.Arguments.Count);
                sb.Append("<");
                for (var i = 0; i < type.Arguments.Count; i++)
                {
                    var curGen = type.Arguments[i];
                    if (curGen.Arguments.Count > 0)
                    {
                        FormatExpandedGeneric(sb, curGen);
                    }
                    else
                    {
                        sb.Append(curGen.GenericTypeName);
                    }
                    if (i != type.Arguments.Count - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.Append(">");
            }
        }

        private class GenericType
        {
            public readonly List<GenericType> Arguments = new List<GenericType>();

            public readonly GenericType Parent;

            public string GenericTypeName;

            public GenericType(string typeName, GenericType parent)
            {
                this.GenericTypeName = typeName;

                var idx = typeName.IndexOf('`');
                if (idx != -1)
                {
                    this.GenericTypeName = typeName.Substring(0, idx);
                }
                this.Parent = parent;
            }
        }
    }
}