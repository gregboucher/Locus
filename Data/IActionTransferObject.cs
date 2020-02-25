namespace Locus.Data
{
    public interface IActionTransferObject<T> where T : class
    {
        T Model { get; set; }
    }
}
