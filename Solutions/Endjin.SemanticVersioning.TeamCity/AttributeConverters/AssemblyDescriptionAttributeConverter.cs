namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Reflection;

    #endregion

    public class AssemblyDescriptionAttributeConverter
    {
        public CodeAttributeDeclaration Convert(AssemblyDescriptionAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.Description)));
        }
    }
}