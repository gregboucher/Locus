using System.Collections.Generic;
using System;
using Locus.ViewModels;
using Locus.Models;
using System.Data;

namespace Locus.Data
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

        QualifiedUser CreateNewUser(UserCreatePostModel model);
        QualifiedUser EditExistingUser(UserEditPostModel model);
        void AddNewAssignment(IDbConnection db, IDbTransaction transaction, Dictionary<int, int> groupDictionary, List<GroupOfAssignments> groups, NewAssignment _model, int userId);

        string CheckStatus(DateTime dueDate);
    }
}