namespace Endjin.SemanticVersioning.TeamCity
{
    #region Using Directives

    using System;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Reflection;

    #endregion

    public class CodeGen
    {
        public void GenerateVersionDetailsAssembly(string currentAssembly, string configuration, string assemblyVersion, string productVersion, string semanticVersion)
        {
            var assembly = Assembly.LoadFile(currentAssembly);
            var attributes = assembly.GetAssemblyAttributes();

            var codeCompileUnit = new CodeCompileUnit();

            foreach (var attribute in attributes)
            {
                //codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression())));
            }

            var x = new CodeAttributeDeclaration(
                new CodeTypeReference(typeof(AssemblyConfigurationAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(configuration)));

            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyConfigurationAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(configuration))));
            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyFileVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(productVersion))));
            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyInformationalVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(productVersion))));
            codeCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(AssemblyVersionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(assemblyVersion))));

            using (var provider = CodeDomProvider.CreateProvider("CSharp"))
            {
                var parameters = new CompilerParameters
                {
                    GenerateExecutable = false,
                    OutputAssembly = "AutoGen.dll"
                };

                var compilerResults = provider.CompileAssemblyFromDom(parameters, codeCompileUnit);

                if (compilerResults.Errors.HasErrors)
                {
                    throw new InvalidOperationException("Failed to generate temporary assembly");
                }
            }

            Console.WriteLine(Assembly.LoadFrom(@"C:\_Projects\endjin\IP\Endjin.Assembly.ChangeDetection\Solutions\Endjin.SemanticVersioning.TeamCity\bin\Debug\AutoGen.dll").CodeBase);
        }
    }
}