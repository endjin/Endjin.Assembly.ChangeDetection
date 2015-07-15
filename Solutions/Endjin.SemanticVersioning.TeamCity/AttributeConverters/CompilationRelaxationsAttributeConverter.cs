namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Runtime.CompilerServices;

    #endregion

    public class CompilationRelaxationsAttributeConverter
    {
        public CodeAttributeDeclaration Convert(CompilationRelaxationsAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.CompilationRelaxations)));
        }
    }
}