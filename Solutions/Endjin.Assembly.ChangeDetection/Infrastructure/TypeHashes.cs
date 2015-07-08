namespace AssemblyDifferences.Infrastructure
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Create a static instance of each class where you want to use tracing.
    ///     It does basically encapsulate the typename and enables fast trace filters.
    /// </summary>
    public class TypeHashes
    {
        private static readonly char[] mySep = { '.' };

        internal int[] myTypeHashes;

        /// <summary>
        ///     Initializes a new instance of the TypeHandle class.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        public TypeHashes(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("typeName");
            }

            this.FullQualifiedTypeName = typeName;

            // Generate from the full qualified type name substring for each part between the . characters.
            // Each substring is then hashed so we can later compare not strings but integer arrays which are super
            // fast! Since this is done only once for a type we can afford doing a little more work here and spare
            // huge amount of comparison time later. 
            // If by a rare incident the hash values would collide with another named type we would have enabled
            // tracing by accident for one more type than intended. 
            var hashes = new List<int>();
            foreach (var substr in this.FullQualifiedTypeName.ToLower().Split(mySep))
            {
                hashes.Add(substr.GetHashCode());
            }
            this.myTypeHashes = hashes.ToArray();
        }

        /// <summary>
        ///     Create a TypeHandle which is used by the Tracer class.
        /// </summary>
        /// <param name="t">Type of your enclosing class.</param>
        public TypeHashes(Type t) : this(CheckInput(t))
        {
        }

        internal string FullQualifiedTypeName { get; private set; }

        private static string CheckInput(Type t)
        {
            if (t == null)
            {
                throw new ArgumentNullException("Type");
            }

            return string.Join(".", t.Namespace, t.Name);
        }
    }
}