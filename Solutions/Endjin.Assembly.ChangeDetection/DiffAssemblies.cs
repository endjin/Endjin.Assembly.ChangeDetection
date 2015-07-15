namespace AssemblyDifferences
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using AssemblyDifferences.Diff;
    using AssemblyDifferences.Infrastructure;
    using AssemblyDifferences.Introspection;
    using AssemblyDifferences.Query;

    public class DiffAssemblies
    {
        private static readonly TypeHashes MyType = new TypeHashes(typeof(DiffAssemblies));

        //protected CommandData myParsedArgs;

        protected void Validate()
        {
            /*base.Detect();

            ValidateFileQuery(myParsedArgs.NewFiles,
                        "-new <filequery> is missing.",
                        "Invalid directory in -new {0} query.",
                        "The -new query {0} did not match any files.");

            ValidateFileQuery(myParsedArgs.OldFiles,
                        "-old <filequery> is missing.",
                        "Not existing directory in -old {0} query.",
                        "The -old query {0} did not match any files.");

            if (myParsedArgs.OutputToExcel)
            {
                AddErrorMessage("Excel output is not supported by this comand");
                SetInvalid();
            }*/
        }

        public AssemblyDiffCollection Execute(List<FileQuery> oldFiles, List<FileQuery> newFiles)
        {
            using (var t = new Tracer(MyType, "Execute"))
            {
                //var removedTypes = 0;
                //var changedTypes = 0;

                var removedFiles = oldFiles.GetNotExistingFilesInOtherQuery(newFiles);
                
                if (removedFiles.Count > 0)
                {
                    foreach (var str in removedFiles)
                    {
                        Console.WriteLine("\t{0}", Path.GetFileName(str));
                    }
                }

                var oldFilesQuery = new HashSet<string>(oldFiles.GetFiles(), new FileNameComparer());
                var newFilesQuery = new HashSet<string>(newFiles.GetFiles(), new FileNameComparer());

                // Get files which are present in one set and the other
                oldFilesQuery.IntersectWith(newFilesQuery);
                //DiffPrinter printer = new DiffPrinter(Out);

                var differences = new AssemblyDiffCollection();

                foreach (var fileName1 in oldFilesQuery)
                {
                    if (fileName1.EndsWith(".XmlSerializers.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        t.Info("Ignore xml serializer dll {0}", fileName1);
                        continue;
                    }

                    var fileName2 = newFiles.GetMatchingFileByName(fileName1);

                    var assemblyV1 = AssemblyLoader.LoadCecilAssembly(fileName1);
                    var assemblyV2 = AssemblyLoader.LoadCecilAssembly(fileName2);

                    if (assemblyV1 != null && assemblyV2 != null)
                    {
                        var differ = new AssemblyDiffer(assemblyV1, assemblyV2);
                        differences = differ.GenerateTypeDiff(QueryAggregator.PublicApiQueries);
                        
                        //removedTypes += differences.AddedRemovedTypes.RemovedCount;
                        //changedTypes += differences.ChangedTypes.Count;

                        /*if (diff.AddedRemovedTypes.Count > 0 || diff.ChangedTypes.Count > 0)
                        {
                            // Out.WriteLine("{0} has {1} changes", Path.GetFileName(fileName1), diff.AddedRemovedTypes.Count + diff.ChangedTypes.Sum(type => type.Events.Count + type.Fields.Count + type.Interfaces.Count + type.Methods.Count));

                            // printer.Print(diff);
                            // Out.WriteLine("From {0} assemblies were {1} types removed and {2} changed.", myParsedArgs.Queries1.GetFiles().Count(), removedTypes, changedTypes);
                         * 
                        }*/
                    }
                }

                return differences;
            }
        }
    }
}