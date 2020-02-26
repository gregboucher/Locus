namespace Locus.Data
{
    public interface IComplexTransferObject<T> where T : class
    {
        T Model { get; set; }
    }
}
