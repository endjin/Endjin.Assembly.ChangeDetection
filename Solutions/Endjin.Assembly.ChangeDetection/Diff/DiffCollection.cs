using System.Collections.Generic;
using System.Linq;

namespace Endjin.Assembly.ChangeDetection.Diff
{
    public class DiffCollection<T> : List<DiffResult<T>>
    {
        public int AddedCount
        {
            get
            {
                var added = 0;
                foreach (var obj in this)
                {
                    if (obj.Operation.IsAdded)
                    {
                        added++;
                    }
                }

                return added;
            }
        }

        public int RemovedCount
        {
            get
            {
                var removed = 0;
                foreach (var obj in this)
                {
                    if (obj.Operation.IsRemoved)
                    {
                        removed++;
                    }
                }

                return removed;
            }
        }

        public IEnumerable<DiffResult<T>> Added
        {
            get
            {
                foreach (var obj in this)
                {
                    if (obj.Operation.IsAdded)
                    {
                        yield return obj;
                    }
                }
            }
        }

        public IEnumerable<DiffResult<T>> Removed
        {
            get
            {
                foreach (var obj in this)
                {
                    if (obj.Operation.IsRemoved)
                    {
                        yield return obj;
                    }
                }
            }
        }

        public List<T> RemovedList
        {
            get
            {
                return (from type in this.Removed select type.ObjectV1).ToList();
            }
        }

        public List<T> AddedList
        {
            get
            {
                return (from type in this.Added select type.ObjectV1).ToList();
            }
        }
    }
}