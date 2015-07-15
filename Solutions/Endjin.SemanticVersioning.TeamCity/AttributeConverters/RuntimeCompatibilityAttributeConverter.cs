namespace Endjin.SemanticVersioning.TeamCity.AttributeConverters
{
    #region Using Directives

    using System.CodeDom;
    using System.Runtime.CompilerServices;

    #endregion

    public class RuntimeCompatibilityAttributeConverter
    {
        public CodeAttributeDeclaration Convert(RuntimeCompatibilityAttribute attribute)
        {
            return new CodeAttributeDeclaration(new CodeTypeReference(attribute.GetType()));
        }
    }
}