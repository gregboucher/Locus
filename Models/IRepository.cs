using System.Collections.Generic;

namespace Locus.Models
{
    public interface IRepository
    {
        IEnumerable<Group> GetAssignmentsByGroup();
        int AssignedUserCount();
        int DueTodayCount();
        int OverdueCount();
        Asset GetAsset(string serialNumber);
    }
}