namespace AssemblyDifferences.Rules
{
    using AssemblyDifferences.Diff;

    public interface IRule
    {
        bool Detect(AssemblyDiffCollection assemblyDiffCollection);
    }
}