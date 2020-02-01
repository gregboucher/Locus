using System.Collections.Generic;
using System;

namespace Locus.Models
{
    public interface IRepository
    {
        IEnumerable<GroupOfUsers> GetAssignmentsByGroup();
        int CountDueToday();
        int CountOverdue();
        int CountUsersCreatedToday();

        IEnumerable<GroupOfModels> GetModelsByGroup(int? id);
        IEnumerable<Role> GetAllRoles();
        UserDetails GetUserDetails(int id);
        string CheckStatus(DateTime dueDate);
        //int GetAssignedModels();
    }
}