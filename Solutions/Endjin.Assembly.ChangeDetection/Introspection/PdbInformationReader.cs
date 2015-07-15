namespace AssemblyDifferences.Introspection
{
    #region Using Directives

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using AssemblyDifferences.Infrastructure;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Pdb;

    #endregion

    public class PdbInformationReader : IDisposable
    {
        private static readonly TypeHashes myType = new TypeHashes(typeof(PdbInformationReader));
        private readonly PdbReaderProvider myPdbFactory = new PdbReaderProvider();
        private readonly Dictionary<string, ISymbolReader> myFile2PdbMap = new Dictionary<string, ISymbolReader>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> myFailedPdbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string mySymbolServer;
        private readonly PdbDownLoader myDownLoader = new PdbDownLoader();

        public PdbInformationReader()
        {
        }

        public PdbInformationReader(string symbolServer)
        {
            this.mySymbolServer = symbolServer;
        }

        public void ReleasePdbForModule(ModuleDefinition module)
        {
            string fileName = module.Assembly.MainModule.FullyQualifiedName;
            ISymbolReader reader;

            if (this.myFile2PdbMap.TryGetValue(fileName, out reader))
            {
                reader.Dispose();
                this.myFile2PdbMap.Remove(fileName);
            }
        }

        public ISymbolReader LoadPdbForModule(ModuleDefinition module)
        {
            using (Tracer t = new Tracer(myType, "LoadPdbForModule"))
            {
                string fileName = module.Assembly.MainModule.FullyQualifiedName;
                t.Info("Module file name: {0}", fileName);
                ISymbolReader reader = null;

                if (!this.myFile2PdbMap.TryGetValue(fileName, out reader))
                {
                    if (this.myFailedPdbs.Contains(fileName))
                    {
                        t.Warning("This pdb could not be successfully downloaded");
                        return reader;
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        try
                        {
                            reader = this.myPdbFactory.GetSymbolReader(module, fileName);
                            this.myFile2PdbMap[fileName] = reader;
                            break;
                        }
                        catch (Exception ex)
                        {
                            t.Error(Level.L3, ex, "Pdb did not match or it is not present");

                            string pdbFileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".pdb");
                            try
                            {
                                File.Delete(pdbFileName);
                            }
                            catch (Exception delex)
                            {
                                t.Error(Level.L2, delex, "Could not delete pdb {0}", pdbFileName);
                            }

                            // When we have symbol server we try to make us of it for matches.
                            if (String.IsNullOrEmpty(this.mySymbolServer))
                            {
                                break;
                            }

                            t.Info("Try to download pdb from symbol server {0}", this.mySymbolServer);
                            bool bDownloaded = this.myDownLoader.DownloadPdbs(new FileQuery(fileName), this.mySymbolServer);
                            t.Info("Did download pdb {0} from symbol server with return code: {1}", fileName, bDownloaded);

                            if (bDownloaded == false || i == 1) // second try did not work out as well
                            {
                                this.myFailedPdbs.Add(fileName);
                                break;
                            }
                        }
                    }
                }

                return reader;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            // release pdbs
            foreach (ISymbolReader reader in this.myFile2PdbMap.Values)
            {
                reader.Dispose();
            }
            this.myFile2PdbMap.Clear();
        }

        #endregion

        /// <summary>
        /// Try to get the file name where the type is defined from the pdb via walking
        /// through some methods
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public KeyValuePair<string, int> GetFileLine(TypeDefinition type)
        {
            KeyValuePair<string, int> fileLine = new KeyValuePair<string, int>("", 0);

            for (int i = 0; i < type.Methods.Count; i++)
            {
                fileLine = this.GetFileLine(type.Methods[i].Body);
                if (!String.IsNullOrEmpty(fileLine.Key))
                {
                    break;
                }
            }
            return fileLine;
        }

        public KeyValuePair<string, int> GetFileLine(MethodDefinition method)
        {
            return this.GetFileLine(method.Body);
        }

        public KeyValuePair<string, int> GetFileLine(MethodBody body)
        {
            if (body != null)
            {
                var symbolReader = this.LoadPdbForModule(body.Method.DeclaringType.Module);

                if (symbolReader != null)
                {
                    foreach (Instruction ins in body.Instructions)
                    {
                        if (ins.SequencePoint != null)
                        {
                            return new KeyValuePair<string, int>(this.PatchDriveLetter(ins.SequencePoint.Document.Url), 0);
                        }
                    }
                }
            }

            return new KeyValuePair<string, int>("", 0);
        }

        private bool HasValidFileAndLineNumber(Instruction ins)
        {
            bool lret = true;
            if (ins == null)
            {
                lret = false;
            }
            if (lret)
            {
                if (ins.SequencePoint == null)
                {
                    lret = false;
                }
            }

            if (lret)
            {
                if (ins.SequencePoint.StartLine == 0xfeefee)
                {
                    lret = false;
                }
            }

            return lret;
        }

        private Instruction GetILInstructionWithLineNumber(Instruction ins, bool bSearchForward)
        {
            Instruction current = ins;
            if (bSearchForward)
            {
                while (current != null && !this.HasValidFileAndLineNumber(current))
                {
                    current = current.Next;
                }
            }
            else
            {
                while (current != null && !this.HasValidFileAndLineNumber(current))
                {
                    current = current.Previous;
                }
            }

            return current;
        }

        /// <summary>
        /// Get for a specific IL instruction the matching file and line.
        /// </summary>
        /// <param name="ins"></param>
        /// <param name="method"></param>
        /// <param name="bSearchForward">Search the next il instruction first if set to true for the line number from the pdb. If nothing is found we search backward.</param>
        /// <returns></returns>
        public KeyValuePair<string, int> GetFileLine(Instruction ins, MethodDefinition method, bool bSearchForward)
        {
            using (Tracer t = new Tracer(myType, "GetFileLine"))
            {
                t.Info("Try to get file and line info for {0} {1} forwardSearch {2}", method.DeclaringType.FullName, method.Name, bSearchForward);

                var symReader = this.LoadPdbForModule(method.DeclaringType.Module);
                if (symReader != null && method.Body != null)
                {
                    Instruction current = ins;

                    if (bSearchForward)
                    {
                        current = this.GetILInstructionWithLineNumber(ins, true);
                        if (current == null)
                        {
                            current = this.GetILInstructionWithLineNumber(ins, false);
                        }
                    }
                    else
                    {
                        current = this.GetILInstructionWithLineNumber(ins, false);
                        if (current == null)
                        {
                            current = this.GetILInstructionWithLineNumber(ins, true);
                        }
                    }

                    if (current != null)
                    {
                        return new KeyValuePair<string, int>(this.PatchDriveLetter(current.SequencePoint.Document.Url), current.SequencePoint.StartLine);
                    }
                }
                else
                {
                    t.Info("No symbol reader present or method has no body");
                }

                return new KeyValuePair<string, int>("", 0);
            }
        }

        private string PatchDriveLetter(string url)
        {
            string root = Directory.GetDirectoryRoot(Directory.GetCurrentDirectory());
            StringBuilder sb = new StringBuilder(url);
            sb[0] = root[0];
            return sb.ToString();
        }
    }
}