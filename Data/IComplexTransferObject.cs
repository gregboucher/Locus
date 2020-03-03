namespace Locus.Data
{
    public interface IComplexTransferObject<T> where T : class
    {
        T Results { get; set; }
    }
}
