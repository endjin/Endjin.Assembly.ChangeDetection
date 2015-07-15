namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Diagnostics;

    #endregion

    public class DebuggableAttributeConverter
    {
        public CodeAttributeDeclaration Convert(DebuggableAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.DebuggingFlags)));
        }
    }
}