namespace Locus.Models
{
    public class NegativeEditResult : Result
    {
        public string AssetId { get; set; }

        public NegativeEditResult(EditOperation operation)
        {
            AssetId = operation.AssetId;
            Type = operation.Type;
        }
    }
}
