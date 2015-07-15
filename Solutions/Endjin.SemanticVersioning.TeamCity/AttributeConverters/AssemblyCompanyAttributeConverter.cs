namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    using System.CodeDom;
    using System.Reflection;

    public class AssemblyCompanyAttributeConverter
    {
        public CodeAttributeDeclaration Convert(AssemblyCompanyAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()), new CodeAttributeArgument(new CodePrimitiveExpression(attribute.Company)));
        }
    }
}