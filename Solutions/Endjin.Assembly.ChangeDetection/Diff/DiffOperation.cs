namespace Endjin.Assembly.ChangeDetection.Diff
{
    public class DiffOperation
    {
        public DiffOperation(bool isAdded)
        {
            this.IsAdded = isAdded;
        }

        public bool IsAdded { get; private set; }

        public bool IsRemoved
        {
            get
            {
                return !this.IsAdded;
            }
        }
    }
}