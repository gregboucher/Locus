using System.Collections.Generic;
using Locus.ViewModels;
using Locus.Models;
using System.Data;
using System;

namespace Locus.Data
{
    public interface IRepository
    {
        IEnumerable<ListTByCollection<User>> GetUsersByCollection();
        int CountDueToday();
        int CountOverdue();
        int CountUsersCreatedToday();
        int CountLongterm();

        IEnumerable<ListModelsByCollection<Model>> GetModelsByCollection(int? id);
        IEnumerable<Role> GetAllRoles();
        UserDetails GetUserDetails(int id);

        Tuple<IList<IResult>, int> CreateNewUser(UserCreatePostModel model);
        IList<IResult> EditExistingUser(UserEditPostModel model);
        Report GenerateReport(IList<IResult> results, int userId);

        Status UserStatus(int userId, IDbConnection db);
    }
}