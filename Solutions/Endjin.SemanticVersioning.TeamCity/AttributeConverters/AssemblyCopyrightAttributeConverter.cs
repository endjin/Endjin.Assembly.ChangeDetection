namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Reflection;

    #endregion

    public class AssemblyCopyrightAttributeConverter
    {
        public CodeAttributeDeclaration Convert(AssemblyCopyrightAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.Copyright)));
        }
    }
}