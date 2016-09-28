namespace Endjin.Assembly.ChangeDetection.Diff
{
    public class DiffResult<T>
    {
        public DiffResult(T v1, DiffOperation diffType)
        {
            this.ObjectV1 = v1;
            this.Operation = diffType;
        }

        public DiffOperation Operation { get; private set; }

        public T ObjectV1 { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}, {1}", this.ObjectV1, this.Operation.IsAdded ? "added" : "removed");
        }
    }
}