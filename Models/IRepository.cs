using System.Collections.Generic;

namespace Locus.Models
{
    public interface IRepository
    {
        IEnumerable<Assignment> TestFunc();
        IEnumerable<Assignment> GetActiveAssignments();
        Asset GetAsset(string SerialNumber);
    }
}