using System.Collections.Generic;

namespace Locus.Models
{
    public interface IRepository
    {
        IEnumerable<Group_User> GetAssignmentsByGroup();
        int DistinctUsersByGroupCount();
        int DueTodayCount();
        int OverdueCount();
        int CreatedTodayCount();

        IEnumerable<Group_InactiveModel> GetAllInactiveModels();
        IEnumerable<Role> GetAllRoles(); 
        //int GetAssignedModels();
    }
}