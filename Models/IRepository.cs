using System.Collections.Generic;
using System;
using Locus.ViewModels;

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

        void CreateNewUser(UserCreatePostModel model);
        void EditExistingUser(UserEditPostModel model);

        string CheckStatus(DateTime dueDate);
    }
}