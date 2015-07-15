namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Reflection;

    #endregion

    public class AssemblyTrademarkAttributeConverter
    {
        public CodeAttributeDeclaration Convert(AssemblyTrademarkAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.Trademark)));
        }
    }
}