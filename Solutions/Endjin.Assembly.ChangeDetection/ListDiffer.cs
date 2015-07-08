namespace AssemblyDifferences
{
    using System;
    using System.Collections;

    /// <summary>
    ///     Compares two lists and creates two diff lists with added and removed elements
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListDiffer<T>
    {
        private readonly Func<T, T, bool> myIsEqual;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ListDiffer&lt;T&gt;" /> class.
        /// </summary>
        /// <param name="comparer">The comparer function to check for equality in the collections to be compared.</param>
        public ListDiffer(Func<T, T, bool> comparer)
        {
            this.myIsEqual = comparer;
        }

        /// <summary>
        ///     Compare two lists A and B and add the new elements in B to added and the elements elements
        ///     which occur only in A and not in B to the removed collection.
        /// </summary>
        /// <param name="listV1">The list v1.</param>
        /// <param name="listV2">The list v2.</param>
        /// <param name="added">New added elements in version 2</param>
        /// <param name="removed">Removed elements in version 2</param>
        public void Diff(IEnumerable listV1, IEnumerable listV2, Action<T> added, Action<T> removed)
        {
            foreach (T ai in listV1)
            {
                var bIsInList = false;
                foreach (T bi in listV2)
                {
                    if (this.myIsEqual(ai, bi))
                    {
                        bIsInList = true;
                        break;
                    }
                }

                if (!bIsInList)
                {
                    removed(ai);
                }
            }

            foreach (T bi in listV2)
            {
                var bIsInList = false;
                foreach (T ai in listV1)
                {
                    if (this.myIsEqual(bi, ai))
                    {
                        bIsInList = true;
                        break;
                    }
                }

                if (!bIsInList)
                {
                    added(bi);
                }
            }
        }
    }
}