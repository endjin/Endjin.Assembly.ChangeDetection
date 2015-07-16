namespace Endjin.SemanticVersioning.TeamCity
{
    #region Using Directives

    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;

    using Endjin.SemanticVersioning.TeamCity.AttributeConverters;

    #endregion

    public class CodeGenerator
    {
        /// <summary>
        /// Generates a dynamic assembly containing all of the custom attributes from the source assembly and then generates new version number based attributes
        /// which ILMerge can then use to stamp onto the source assembly
        /// </summary>
        /// <param name="currentAssembly"></param>
        /// <param name="assemblyVersion"></param>
        /// <param name="productVersion"></param>
        /// <param name="semanticVersion"></param>
        /// <returns>Path to the dynamic assembly</returns>
        public string GenerateVersionDetailsDynamicAssembly(string currentAssembly, string assemblyVersion, string productVersion, string semanticVersion)
        {
            var assembly = Assembly.LoadFile(currentAssembly);
            var attributes = assembly.GetAssemblyAttributes();
            var codeCompileUnit = new CodeCompileUnit();

            // Loop through known assembly attributes and convert them into AssemblyCustomAttributes
            foreach (var attribute in attributes)
            {
                ConvertAttributeToAssemblyCustomAttribute(attribute, codeCompileUnit);
            }

            // Generate new assembly version attributes with the new semantic version
            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyFileVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(productVersion))));
            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyInformationalVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(productVersion))));
            //codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(assemblyVersion))));

            using (var provider = CodeDomProvider.CreateProvider("CSharp"))
            {
                var dynamicAssemblyPath = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), Path.GetTempFileName()), ".dll");

                var parameters = new CompilerParameters
                {
                    GenerateExecutable = false,
                    OutputAssembly = dynamicAssemblyPath
                };

                var compilerResults = provider.CompileAssemblyFromDom(parameters, codeCompileUnit);

                if (compilerResults.Errors.HasErrors)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (var error in compilerResults.Errors)
                    {
                        sb.Append(error.ToString());
                        sb.Append(Environment.NewLine);
                    }

                    throw new InvalidOperationException("Failed to generate temporary assembly. " + sb.ToString());
                }

                return parameters.OutputAssembly;
            }
        }

        private static void ConvertAttributeToAssemblyCustomAttribute(Attribute attribute, CodeCompileUnit codeCompileUnit)
        {
            switch (attribute.GetType().Name)
            {
                case "AssemblyCompanyAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new AssemblyCompanyAttributeConverter().Convert(attribute as AssemblyCompanyAttribute));
                    break;
                case "AssemblyConfigurationAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new AssemblyConfigurationAttributeConverter().Convert(attribute as AssemblyConfigurationAttribute));
                    break;
                case "AssemblyCopyrightAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new AssemblyCopyrightAttributeConverter().Convert(attribute as AssemblyCopyrightAttribute));
                    break;
                case "AssemblyDescriptionAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new AssemblyDescriptionAttributeConverter().Convert(attribute as AssemblyDescriptionAttribute));
                    break;
                case "AssemblyProductAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new AssemblyProductAttributeConverter().Convert(attribute as AssemblyProductAttribute));
                    break;
                case "AssemblyTitleAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new AssemblyTitleAttributeConverter().Convert(attribute as AssemblyTitleAttribute));
                    break;
                case "AssemblyTrademarkAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new AssemblyTrademarkAttributeConverter().Convert(attribute as AssemblyTrademarkAttribute));
                    break;
                case "CompilationRelaxationsAttribute":
                        codeCompileUnit.AssemblyCustomAttributes.Add(new CompilationRelaxationsAttributeConverter().Convert(attribute as CompilationRelaxationsAttribute));
                        break;
                case "ComVisibleAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new ComVisibleAttributeConverter().Convert(attribute as ComVisibleAttribute));
                    break;
                // TODO: currently throws a compiler exception 
                /*case "DebuggableAttribute":
                        codeCompileUnit.AssemblyCustomAttributes.Add(new DebuggableAttributeConverter().Convert(attribute as DebuggableAttribute));
                        break;*/
                case "GuidAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new GuidAttributeConverter().Convert(attribute as GuidAttribute));
                    break;
                case "RuntimeCompatibilityAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new RuntimeCompatibilityAttributeConverter().Convert(attribute as RuntimeCompatibilityAttribute));
                    break;
                case "TargetFrameworkAttribute":
                    codeCompileUnit.AssemblyCustomAttributes.Add(new TargetFrameworkAttributeConverter().Convert(attribute as TargetFrameworkAttribute));
                    break;
            }
        }
    }
}