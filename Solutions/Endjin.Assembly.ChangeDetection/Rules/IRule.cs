using Endjin.Assembly.ChangeDetection.Diff;

namespace Endjin.Assembly.ChangeDetection.Rules
{
    public interface IRule
    {
        bool Detect(AssemblyDiffCollection assemblyDiffCollection);
    }
}