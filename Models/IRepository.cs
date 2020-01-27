using System.Collections.Generic;

namespace Locus.Models
{
    public interface IRepository
    {
        IEnumerable<Group_User> GetAssignmentsByGroup();
        int DueTodayCount();
        int OverdueCount();
        int UsersCreatedTodayCount();

        IEnumerable<Group_InactiveModel> GetAllInactiveModels();
        IEnumerable<Role> GetAllRoles(); 
        //int GetAssignedModels();
    }
}