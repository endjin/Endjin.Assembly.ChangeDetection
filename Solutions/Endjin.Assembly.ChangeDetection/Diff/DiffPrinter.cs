using System;
using System.IO;
using Endjin.Assembly.ChangeDetection.Introspection;
using Mono.Cecil;

namespace Endjin.Assembly.ChangeDetection.Diff
{
    public class DiffPrinter
    {
        private readonly TextWriter Out;

        /// <summary>
        ///     Print diffs to console
        /// </summary>
        public DiffPrinter()
        {
            this.Out = Console.Out;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DiffPrinter" /> class.
        /// </summary>
        /// <param name="outputStream">The output stream to print the change diff.</param>
        public DiffPrinter(TextWriter outputStream)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }

            this.Out = outputStream;
        }

        internal void Print(AssemblyDiffCollection diff)
        {
            this.PrintAddedRemovedTypes(diff.AddedRemovedTypes);

            if (diff.ChangedTypes.Count > 0)
            {
                foreach (var typeChange in diff.ChangedTypes)
                {
                    this.PrintTypeChanges(typeChange);
                }
            }
        }

        private void PrintTypeChanges(TypeDiff typeChange)
        {
            this.Out.WriteLine("\t" + typeChange.TypeV1.Print());
            if (typeChange.HasChangedBaseType)
            {
                this.Out.WriteLine("\t\tBase type changed: {0} -> {1}", typeChange.TypeV1.IsNotNull(() => typeChange.TypeV1.BaseType.IsNotNull(() => typeChange.TypeV1.BaseType.FullName)), typeChange.TypeV2.IsNotNull(() => typeChange.TypeV2.BaseType.IsNotNull(() => typeChange.TypeV2.BaseType.FullName)));
            }

            if (typeChange.Interfaces.Count > 0)
            {
                foreach (var addedItf in typeChange.Interfaces.Added)
                {
                    this.Out.WriteLine("\t\t+ interface: {0}", addedItf.ObjectV1.FullName);
                }
                foreach (var removedItd in typeChange.Interfaces.Removed)
                {
                    this.Out.WriteLine("\t\t- interface: {0}", removedItd.ObjectV1.FullName);
                }
            }

            foreach (var addedEvent in typeChange.Events.Added)
            {
                this.Out.WriteLine("\t\t+ {0}", addedEvent.ObjectV1.Print());
            }

            foreach (var remEvent in typeChange.Events.Removed)
            {
                this.Out.WriteLine("\t\t- {0}", remEvent.ObjectV1.Print());
            }

            foreach (var addedField in typeChange.Fields.Added)
            {
                this.Out.WriteLine("\t\t+ {0}", addedField.ObjectV1.Print(FieldPrintOptions.All));
            }

            foreach (var remField in typeChange.Fields.Removed)
            {
                this.Out.WriteLine("\t\t- {0}", remField.ObjectV1.Print(FieldPrintOptions.All));
            }

            foreach (var addedMethod in typeChange.Methods.Added)
            {
                this.Out.WriteLine("\t\t+ {0}", addedMethod.ObjectV1.Print(MethodPrintOption.Full));
            }

            foreach (var remMethod in typeChange.Methods.Removed)
            {
                this.Out.WriteLine("\t\t- {0}", remMethod.ObjectV1.Print(MethodPrintOption.Full));
            }
        }

        private void PrintAddedRemovedTypes(DiffCollection<TypeDefinition> diffCollection)
        {
            if (diffCollection.RemovedCount > 0)
            {
                this.Out.WriteLine("\tRemoved {0} public type/s", diffCollection.RemovedCount);
                foreach (var remType in diffCollection.Removed)
                {
                    this.Out.WriteLine("\t\t- {0}", remType.ObjectV1.Print());
                }
            }

            if (diffCollection.AddedCount > 0)
            {
                this.Out.WriteLine("\tAdded {0} public type/s", diffCollection.AddedCount);
                foreach (var addedType in diffCollection.Added)
                {
                    this.Out.WriteLine("\t\t+ {0}", addedType.ObjectV1.Print());
                }
            }
        }
    }
}