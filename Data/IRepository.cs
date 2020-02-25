using System.Collections.Generic;
using Locus.ViewModels;
using Locus.Models;
using System.Data;

namespace Locus.Data
{
    public interface IRepository
    {
        IEnumerable<CollectionOfGenerics<User>> GetUsersByCollection();
        int CountDueToday();
        int CountOverdue();
        int CountUsersCreatedToday();

        IEnumerable<CollectionOfModels<Model>> GetModelsByCollection(int? id);
        IEnumerable<Role> GetAllRoles();
        UserDetails GetUserDetails(int id);

        UserSummary CreateNewUser(UserCreatePostModel model);
        UserSummary EditExistingUser(UserEditPostModel model);

        Status UserStatus(int userId, IDbConnection db);

    }
}