namespace AssemblyDifferences.Rules
{
    #region Using Directives

    using System.Linq;

    using AssemblyDifferences.Diff;

    #endregion

    public class BreakingChangeRule : IRule
    {
        public bool Validate(AssemblyDiffCollection assemblyDiffCollection)
        {
            if (assemblyDiffCollection.AddedRemovedTypes.RemovedCount > 0)
            {
                return false;
            }

            if (assemblyDiffCollection.ChangedTypes.Count > 0)
            {
                foreach (var typeChange in assemblyDiffCollection.ChangedTypes)
                {
                    if (typeChange.HasChangedBaseType)
                    {
                        return false;
                    }

                    if (typeChange.Interfaces.Count > 0)
                    {
                        if (typeChange.Interfaces.Removed.Any())
                        {
                            return false;
                        }
                    }

                    if (typeChange.Events.Removed.Any())
                    {
                        return false;
                    }

                    if (typeChange.Fields.Removed.Any())
                    {
                        return false;
                    }

                    if (typeChange.Methods.Removed.Any())
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}