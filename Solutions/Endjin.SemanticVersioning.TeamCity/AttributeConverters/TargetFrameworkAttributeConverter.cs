namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Runtime.Versioning;

    #endregion

    public class TargetFrameworkAttributeConverter
    {
        public CodeAttributeDeclaration Convert(TargetFrameworkAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.FrameworkName)));
        }
    }
}