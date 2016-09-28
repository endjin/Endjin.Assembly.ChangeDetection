using System;
using System.Collections.Generic;
using System.IO;

namespace Endjin.Assembly.ChangeDetection.Infrastructure
{
    internal class FileNameComparer : IEqualityComparer<string>
    {
        #region IEqualityComparer<string> Members

        public bool Equals(string x, string y)
        {
            return string.Compare(Path.GetFileName(x), Path.GetFileName(y), StringComparison.OrdinalIgnoreCase) == 0;
        }

        public int GetHashCode(string obj)
        {
            return Path.GetFileName(obj).ToLower().GetHashCode();
        }

        #endregion
    }
}