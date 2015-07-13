namespace AssemblyDifferences.Rules
{
    using AssemblyDifferences.Diff;

    public interface IRule
    {
        bool Validate(AssemblyDiffCollection assemblyDiffCollection);
    }
}