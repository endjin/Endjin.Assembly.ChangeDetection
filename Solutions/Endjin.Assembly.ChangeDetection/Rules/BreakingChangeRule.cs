namespace AssemblyDifferences.Rules
{
    #region Using Directives

    using System.Linq;

    using AssemblyDifferences.Diff;

    #endregion

    public class BreakingChangeRule : IRule
    {
        public bool Detect(AssemblyDiffCollection assemblyDiffCollection)
        {
            if (assemblyDiffCollection.AddedRemovedTypes.RemovedCount > 0)
            {
                return true;
            }

            if (assemblyDiffCollection.ChangedTypes.Count > 0)
            {
                foreach (var typeChange in assemblyDiffCollection.ChangedTypes)
                {
                    if (typeChange.HasChangedBaseType)
                    {
                        return true;
                    }

                    if (typeChange.Interfaces.Count > 0)
                    {
                        if (typeChange.Interfaces.Removed.Any())
                        {
                            return true;
                        }
                    }

                    if (typeChange.Events.Removed.Any())
                    {
                        return true;
                    }

                    if (typeChange.Fields.Removed.Any())
                    {
                        return true;
                    }

                    if (typeChange.Methods.Removed.Any())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}