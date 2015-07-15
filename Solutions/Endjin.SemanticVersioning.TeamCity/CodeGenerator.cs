namespace Endjin.SemanticVersioning.TeamCity
{
    #region Using Directives

    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;

    using Endjin.SemanticVersioning.TeamCity.AttributeConverters;

    #endregion

    public class CodeGenerator
    {
        public void GenerateVersionDetailsAssembly(string currentAssembly, string configuration, string assemblyVersion, string productVersion, string semanticVersion)
        {
            var assembly = Assembly.LoadFile(currentAssembly);
            var attributes = assembly.GetAssemblyAttributes();

            var codeCompileUnit = new CodeCompileUnit();

            foreach (var attribute in attributes)
            {
                switch (attribute.GetType().Name)
                {
                    case "AssemblyCompanyAttribute":
                        codeCompileUnit.AssemblyCustomAttributes.Add(new AssemblyCompanyAttributeConverter().Convert(attribute as AssemblyCompanyAttribute));
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
                    /*case "CompilationRelaxationsAttribute":
                        codeCompileUnit.AssemblyCustomAttributes.Add(new CompilationRelaxationsAttributeConverter().Convert(attribute as CompilationRelaxationsAttribute));
                        break;*/
                    case "ComVisibleAttribute":
                        codeCompileUnit.AssemblyCustomAttributes.Add(new ComVisibleAttributeConverter().Convert(attribute as ComVisibleAttribute));
                        break;
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

            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyConfigurationAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(configuration))));
            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyFileVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(productVersion))));
            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyInformationalVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(productVersion))));
            //codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(assemblyVersion))));

            using (var provider = CodeDomProvider.CreateProvider("CSharp"))
            {
                var parameters = new CompilerParameters
                {
                    GenerateExecutable = false,
                    OutputAssembly = Guid.NewGuid() + ".dll"
                };

                var compilerResults = provider.CompileAssemblyFromDom(parameters, codeCompileUnit);

                if (compilerResults.Errors.HasErrors)
                {
                    throw new InvalidOperationException("Failed to generate temporary assembly");
                }
            }
        }
    }
}