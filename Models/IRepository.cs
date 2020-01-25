using System.Collections.Generic;

namespace Locus.Models
{
    public interface IRepository
    {
        IEnumerable<Group> GetAssignmentsByGroup();
        int DistinctUsersByGroupCount();
        int DueTodayCount();
        int OverdueCount();
        int CreatedTodayCount();
    }
}