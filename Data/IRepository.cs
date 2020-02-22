using System.Collections.Generic;
using Locus.ViewModels;
using Locus.Models;

namespace Locus.Data
{
    public interface IRepository
    {
        IEnumerable<GroupedUsers> GetAssignmentsByGroup();
        int CountDueToday();
        int CountOverdue();
        int CountUsersCreatedToday();

        IEnumerable<GroupedModels> GetModelsByGroup(int? id);
        IEnumerable<Role> GetAllRoles();
        UserDetails GetUserDetails(int id);

        UserSummary CreateNewUser(UserCreatePostModel model);
        UserSummary EditExistingUser(UserEditPostModel model);
    }
}