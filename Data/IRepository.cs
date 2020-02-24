using System.Collections.Generic;
using Locus.ViewModels;
using Locus.Models;
using System.Data;

namespace Locus.Data
{
    public interface IRepository
    {
        IEnumerable<GroupTByDivision<User>> GetAssignmentsByGroup();
        int CountDueToday();
        int CountOverdue();
        int CountUsersCreatedToday();

        IEnumerable<GroupedModels> GetModelsByGroup(int? id);
        IEnumerable<Role> GetAllRoles();
        UserDetails GetUserDetails(int id);

        UserSummary CreateNewUser(UserCreatePostModel model);
        UserSummary EditExistingUser(UserEditPostModel model);

        Status UserStatus(int userId, IDbConnection db);

    }
}