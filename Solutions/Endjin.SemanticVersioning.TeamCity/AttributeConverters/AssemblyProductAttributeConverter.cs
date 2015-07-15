namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Reflection;

    #endregion

    public class AssemblyProductAttributeConverter
    {
        public CodeAttributeDeclaration Convert(AssemblyProductAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.Product)));
        }
    }
}