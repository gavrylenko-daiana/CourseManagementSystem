using System.Security.Claims;
using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using DAL.Interfaces;
using DAL.Repository;
using Microsoft.AspNetCore.Identity;

namespace BLL.Services;

public class UserService : GenericService<AppUser>, IUserService
{
    private readonly UserManager<AppUser> _userManager;

    public UserService(UnitOfWork unitOfWork, UserManager<AppUser> userManager) : base(unitOfWork,
        unitOfWork.UserRepository)
    {
        _userManager = userManager;
    }

    public async Task<Result<AppUser>> GetInfoUserByCurrentUserAsync(ClaimsPrincipal currentUser)
    {
        if (currentUser == null)
        {
            return new Result<AppUser>(false, $"{nameof(currentUser)} does not exist");
        }

        var result = await GetCurrentUser(currentUser);

        if (result.IsSuccessful)
        {
            var user = result.Data;
            var appUser = await GetUserAfterMapping(user);

            return new Result<AppUser>(true, appUser);
        }
        else
        {
            return new Result<AppUser>(false, $"{nameof(currentUser)} does not exist");
        }
    }

    public async Task<Result<AppUser>> GetInfoUserByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new Result<AppUser>(false, $"{nameof(id)} does not exist");
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return new Result<AppUser>(false, $"{nameof(user)} does not exist");
        }

        var appUser = await GetUserAfterMapping(user);

        return new Result<AppUser>(true, appUser);
    }

    private Task<AppUser> GetUserAfterMapping(AppUser currentUser)
    {
        var user = new AppUser();
        currentUser.MapTo(user);

        return Task.FromResult(user);
    }

    public async Task<Result<bool>> EditUserAsync(ClaimsPrincipal currentUser, AppUser editUserViewModel)
    {
        if (editUserViewModel == null)
        {
            return new Result<bool>(false, $"{nameof(editUserViewModel)} does not exist");
        }

        if (currentUser == null)
        {
            return new Result<bool>(false, $"{nameof(currentUser)} does not exist");
        }

        var result = await GetCurrentUser(currentUser);

        if (result.IsSuccessful)
        {
            var user = result.Data;

            user.UserName = editUserViewModel.FirstName + editUserViewModel.LastName;
            user.FirstName = editUserViewModel.FirstName;
            user.LastName = editUserViewModel.LastName;
            user.BirthDate = editUserViewModel.BirthDate!;
            user.University = editUserViewModel.University!;
            user.Telegram = editUserViewModel.Telegram!;
            user.GitHub = editUserViewModel.GitHub!;
            user.Email = editUserViewModel.Email;

            await _userManager.UpdateAsync(user);

            return new Result<bool>(true);
        }
        else
        {
            return new Result<bool>(false, $"Failed to get {nameof(result.Data)} - Message: {result.Message}");
        }
    }

    public async Task<Result<bool>> CheckPasswordAsync(ClaimsPrincipal currentUser, string currentPassword, string newPassword)
    {
        var result = await GetCurrentUser(currentUser);

        if (result.IsSuccessful)
        {
            var user = result.Data;

            var checkPassword = await _userManager.CheckPasswordAsync(user, currentPassword);

            if (checkPassword)
            {
                user.PasswordHash =
                    _userManager.PasswordHasher.HashPassword(user, newPassword);
                await _userManager.UpdateAsync(user);

                return new Result<bool>(true);
            }
            else
            {
                return new Result<bool>(false, $"Failed to check password - Message: {result.Message}");
            }
        }
        else
        {
            return new Result<bool>(false, $"Failed to get {nameof(result.Data)} - Message: {result.Message}");
        }
    }

    public async Task<Result<AppUser>> FindByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return new Result<AppUser>(false, $"Failed to get {nameof(user)}");
        }
        
        return new Result<AppUser>(true, user);
    }

    public async Task<Result<AppUser>> GetCurrentUser(ClaimsPrincipal user)
    {
        var appUser = await _userManager.GetUserAsync(user);

        if (appUser == null)
        {
            return new Result<AppUser>(false, $"{nameof(appUser)} does not exist");
        }

        return new Result<AppUser>(true, appUser);
    }
}