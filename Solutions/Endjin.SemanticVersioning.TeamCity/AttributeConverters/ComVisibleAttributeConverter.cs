namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Runtime.InteropServices;

    #endregion

    public class ComVisibleAttributeConverter
    {
        public CodeAttributeDeclaration Convert(ComVisibleAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.Value)));
        }
    }
}