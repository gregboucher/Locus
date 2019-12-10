using System.Collections.Generic;

namespace Locus.Models
{
    public interface IRepository
    {
        IEnumerable<Group> GetAssignmentsByGroup();
        int AssignedAssetCount();
        int DueTodayCount();
        int OverdueCount();
        Asset GetAsset(string serialNumber);
    }
}