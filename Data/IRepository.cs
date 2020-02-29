using System.Collections.Generic;
using Locus.ViewModels;
using Locus.Models;
using System.Data;

namespace Locus.Data
{
    public interface IRepository
    {
        IEnumerable<ListTByCollection<User>> GetUsersByCollection();
        int CountDueToday();
        int CountOverdue();
        int CountUsersCreatedToday();
        int CountIndefinite();

        IEnumerable<ListModelsByCollection<Model>> GetModelsByCollection(int? id);
        IEnumerable<Role> GetAllRoles();
        UserDetails GetUserDetails(int id);

        Report CreateNewUser(UserCreatePostModel model);
        Report EditExistingUser(UserEditPostModel model);
        Report GenerateReport(List<IReportItem>, int userId);

        Status UserStatus(int userId, IDbConnection db);
    }
}