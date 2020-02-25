namespace Locus.Data
{
    public class ActionTransferObject<T> : IActionTransferObject<T> where T : class
    {
        public T Model { get; set; }
    }
}