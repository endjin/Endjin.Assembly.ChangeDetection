namespace AssemblyDifferences.Rules
{
    using AssemblyDifferences.Diff;

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
                        //this.Out.WriteLine("\t\tBase type changed: {0} -> {1}", typeChange.TypeV1.IsNotNull(() => typeChange.TypeV1.BaseType.IsNotNull(() => typeChange.TypeV1.BaseType.FullName)), typeChange.TypeV2.IsNotNull(() => typeChange.TypeV2.BaseType.IsNotNull(() => typeChange.TypeV2.BaseType.FullName)));
                    }

                    if (typeChange.Interfaces.Count > 0)
                    {
                        foreach (var removedItd in typeChange.Interfaces.Removed)
                        {
                            return false;
                            //this.Out.WriteLine("\t\t- interface: {0}", removedItd.ObjectV1.FullName);
                        }
                    }

                    /*foreach (var addedEvent in typeChange.Events.Added)
                    {
                        //this.Out.WriteLine("\t\t+ {0}", addedEvent.ObjectV1.Print());
                    }*/

                    foreach (var remEvent in typeChange.Events.Removed)
                    {
                        return false;
                        //this.Out.WriteLine("\t\t- {0}", remEvent.ObjectV1.Print());
                    }

                    /*foreach (var addedField in typeChange.Fields.Added)
                    {
                        //this.Out.WriteLine("\t\t+ {0}", addedField.ObjectV1.Print(FieldPrintOptions.All));
                    }*/

                    foreach (var remField in typeChange.Fields.Removed)
                    {
                        return false;
                        //this.Out.WriteLine("\t\t- {0}", remField.ObjectV1.Print(FieldPrintOptions.All));
                    }

                    /*foreach (var addedMethod in typeChange.Methods.Added)
                    {
                        //this.Out.WriteLine("\t\t+ {0}", addedMethod.ObjectV1.Print(MethodPrintOption.Full));
                    }*/

                    foreach (var remMethod in typeChange.Methods.Removed)
                    {
                        return false;
                        //this.Out.WriteLine("\t\t- {0}", remMethod.ObjectV1.Print(MethodPrintOption.Full));
                    }
                }
            }

            return true;
        }
    }
}