namespace AssemblyDifferences.Query.usagequeries
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using AssemblyDifferences.Infrastructure;
    using AssemblyDifferences.Introspection;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class UsageQueryAggregator : IDisposable
    {
        private static readonly TypeHashes myType = new TypeHashes(typeof(UsageQueryAggregator));

        private readonly TypeQuery myTypeQuery;

        private readonly List<UsageVisitor> myVisitors = new List<UsageVisitor>();

        protected HashSet<string> myAssemblyReferencesOfInterest = new HashSet<string>();

        protected PdbInformationReader myPdbReader;

        /// <summary>
        ///     Aggregates the matches from usage queries.
        /// </summary>
        public UsageQueryAggregator() : this(false)
        {
        }

        /// <summary>
        ///     Aggregates the matches from the usage queries.
        /// </summary>
        /// <param name="bReadPdbs">if set to <c>true</c> [you get file and line information for the matches].</param>
        public UsageQueryAggregator(bool bReadPdbs)
        {
            this.MethodMatches = new List<QueryResult<MethodDefinition>>();
            this.TypeMatches = new List<QueryResult<TypeDefinition>>();
            this.myTypeQuery = new TypeQuery();
            this.FieldMatches = new List<QueryResult<FieldDefinition>>();
            this.AssemblyMatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (bReadPdbs)
            {
                this.myPdbReader = new PdbInformationReader();
            }
        }

        /// <summary>
        ///     Aggregates the matches from the usage queries.
        /// </summary>
        /// <param name="symbolServer">
        ///     To get file and line info the pdbs are read. If the pdb does not match it contacts the
        ///     symbol server to look for matching pdb and downloads it.
        /// </param>
        public UsageQueryAggregator(string symbolServer) : this(true)
        {
            if (this.myPdbReader != null)
            {
                this.myPdbReader.Dispose();
                this.myPdbReader = null;
            }

            this.myPdbReader = new PdbInformationReader(symbolServer);
        }

        public UsageQueryAggregator(TypeQuery query) : this(true)
        {
            this.myTypeQuery = query;
        }

        public List<QueryResult<MethodDefinition>> MethodMatches { get; private set; }

        public List<QueryResult<TypeDefinition>> TypeMatches { get; private set; }

        public HashSet<string> AssemblyMatches { get; private set; }

        public List<QueryResult<FieldDefinition>> FieldMatches { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            if (this.myPdbReader != null)
            {
                Tracer.Info(Level.L3, myType, "Dispose", "Release PDB Reader");
                // release pdbs
                this.myPdbReader.Dispose();
            }
        }

        #endregion

        public void Clear()
        {
            this.MethodMatches.Clear();
            this.TypeMatches.Clear();
            this.FieldMatches.Clear();
            this.AssemblyMatches.Clear();
        }

        public void AddVisitScope(string dllName)
        {
            var fileName = Path.GetFileName(dllName).Replace(".dll", "").Replace(".exe", "");
            Tracer.Info(Level.L2, myType, "AddVisitScope", "Add assembly {0} to list of analyzed assemblies", fileName);

            this.myAssemblyReferencesOfInterest.Add(fileName);
        }

        private bool ModuleReferencesAssemblyOrSelf(ModuleDefinition module)
        {
            var lret = false;

            foreach (AssemblyNameReference assemblyRef in module.AssemblyReferences)
            {
                this.myVisitors.ForEach(vis => vis.VisitAssemblyReference(assemblyRef, module.Assembly));
                if (this.myAssemblyReferencesOfInterest.Contains(assemblyRef.Name))
                {
                    lret = true;
                    break;
                }
            }

            // Query for something in itself
            if (!lret && this.myAssemblyReferencesOfInterest.Contains(module.Assembly.Name.Name))
            {
                lret = true;
            }

            return lret;
        }

        public void AddQuery(UsageVisitor visitor)
        {
            this.myVisitors.Add(visitor);
        }

        /// <summary>
        ///     Analyzes the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public void Analyze(AssemblyDefinition assembly)
        {
            using (var t = new Tracer(myType, "Analyze"))
            {
                if (this.myAssemblyReferencesOfInterest.Count == 0 && this.myVisitors.Count > 0)
                {
                    throw new InvalidOperationException("Assembly reference list is empty. The check if the current assembly does reference one of the assemblies can therefore never succeed.  Please call AddVisitScope(string dllName) within your Query ctor or on the Aggregator itself before analysing assemblies. The check is done to prevent searching inside assemblies which do not use parts of other assemblies for performance reasons.");
                }

                t.Info("Analyzing assembly {0}", assembly.Name);
                foreach (ModuleDefinition mod in assembly.Modules)
                {
                    // skip assemblies which do not reference the given assembly
                    // but do include itself into query
                    if (!this.ModuleReferencesAssemblyOrSelf(mod))
                    {
                        t.Info("Current assembly does not reference any assemblies that are checked. Skip other checks.");
                        continue;
                    }

                    foreach (var type in this.myTypeQuery.GetTypes(assembly))
                    {
                        this.myVisitors.ForEach(vis => vis.VisitType(type));

                        foreach (var field in FieldQuery.AllFieldsIncludingCompilerGenerated.GetMatchingFields(type))
                        {
                            this.myVisitors.ForEach(vis => vis.VisitField(field));
                        }

                        foreach (var method in MethodQuery.AllMethods.GetMethods(type))
                        {
                            if (method.HasBody)
                            {
                                this.myVisitors.ForEach(vis => vis.VisitMethodBody(method.Body));
                                if (method.Body.Variables != null)
                                {
                                    this.myVisitors.ForEach(vis => vis.VisitLocals(method.Body.Variables, method));
                                }
                            }
                            this.myVisitors.ForEach(vis => vis.VisitMethod(method));
                        }
                    }

                    if (this.myPdbReader != null)
                    {
                        this.myPdbReader.ReleasePdbForModule(mod);
                    }
                }
            }
        }

        public void AddMatch(Instruction ins, MethodDefinition method, bool bSearchForward, MatchContext context)
        {
            using (var t = new Tracer(myType, "AddMatch_Instruction"))
            {
                t.Info("Add match for instruction {0} in method {1} SearchForward {2}", ins, method, bSearchForward);
                var queryResult = this.GetResultWithFileLineIfEnabled(ins, method, bSearchForward, context);
                this.AddMatch(method.DeclaringType.Module.Assembly);
                this.MethodMatches.Add(queryResult);
            }
        }

        public void AddMatch(AssemblyDefinition assembly)
        {
            Tracer.Info(Level.L2, myType, "AddMatch_Assembly", "Add matching assembly {0}", assembly.Name);
            this.AssemblyMatches.Add(assembly.MainModule.Name);
        }

        public void AddMatch(TypeDefinition typeDef, MatchContext context)
        {
            using (var t = new Tracer(myType, "AddMatch_Type"))
            {
                t.Info("Add type {0} defined in {1}", typeDef.FullName, typeDef.Module.Assembly.Name);
                this.AddMatch(typeDef.Module.Assembly);
                var fileLine = new KeyValuePair<string, int>("", 0);
                if (this.myPdbReader != null)
                {
                    fileLine = this.myPdbReader.GetFileLine(typeDef);
                }

                this.TypeMatches.Add(new QueryResult<TypeDefinition>(typeDef, fileLine.Key, fileLine.Value, context));
            }
        }

        public void AddMatch(FieldDefinition field, MatchContext context)
        {
            using (var t = new Tracer(myType, "AddMatch_Field"))
            {
                t.Info("Add field {0} {1} defined in {2}", field.DeclaringType.FullName, field.Name, field.DeclaringType.Module.Assembly.Name);
                this.AddMatch(field.DeclaringType.Module.Assembly);
                var declType = (TypeDefinition)field.DeclaringType;
                var fileLine = new KeyValuePair<string, int>("", 0);
                if (this.myPdbReader != null)
                {
                    fileLine = this.myPdbReader.GetFileLine(declType);
                }
                this.FieldMatches.Add(new QueryResult<FieldDefinition>(field, fileLine.Key, fileLine.Value, context));
            }
        }

        private bool hasLoadedPdbInfos(MethodBody body)
        {
            foreach (Instruction inst in body.Instructions)
            {
                if (inst.SequencePoint != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Gets the result with file line if enabled.
        /// </summary>
        /// <param name="ins">The ins.</param>
        /// <param name="method">The method.</param>
        /// <param name="bSearchForward">
        ///     Search for the line number forward until the next matching instructions with pdb info.
        ///     Otherwise we first search backward.
        /// </param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private QueryResult<MethodDefinition> GetResultWithFileLineIfEnabled(Instruction ins, MethodDefinition method, bool bSearchForward, MatchContext context)
        {
            QueryResult<MethodDefinition> ret = null;
            if (this.myPdbReader != null)
            {
                var fileLine = this.myPdbReader.GetFileLine(ins, method, bSearchForward);
                ret = new QueryResult<MethodDefinition>(method, fileLine.Key, fileLine.Value, context);
            }
            else
            {
                ret = new QueryResult<MethodDefinition>(method, "", 0, context);
            }

            return ret;
        }

        internal void AddMatch(MethodDefinition method, MatchContext context)
        {
            if (method.HasBody && method.Body.Instructions.Count > 0)
            {
                this.AddMatch(method.Body.Instructions[0], method, true, context);
            }
            else
            {
                if (this.myPdbReader != null)
                {
                    var fileLine = this.myPdbReader.GetFileLine((TypeDefinition)method.DeclaringType);
                    this.MethodMatches.Add(new QueryResult<MethodDefinition>(method, fileLine.Key, fileLine.Value, context));
                }
                else
                {
                    this.MethodMatches.Add(new QueryResult<MethodDefinition>(method, "", 0, context));
                }
            }
        }
    }
}