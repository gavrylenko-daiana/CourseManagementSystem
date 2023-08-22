using System.Linq.Expressions;
using BLL.Interfaces;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using DAL.Repository;
using Microsoft.AspNetCore.Identity;

namespace BLL.Services;

public class GroupService : GenericService<Group>, IGroupService
{
    private readonly IUserGroupService _userGroupService;
    private readonly IEducationMaterialService _educationMaterial;
    private readonly IUserService _userService;

    public GroupService(UnitOfWork unitOfWork, IUserGroupService userGroupService, 
        IEducationMaterialService educationMaterial, IUserService userService) 
            : base(unitOfWork, unitOfWork.GroupRepository)
    {
        _userGroupService = userGroupService;
        _educationMaterial = educationMaterial;
        _userService = userService;
    }

    public async Task<Result<bool>> CreateGroup(Group group, AppUser currentUser)
    {
        if (group == null)
        {
            return new Result<bool>(false, $"{nameof(group)} not found");
        }

        if (currentUser == null)
        {
            return new Result<bool>(false, $"{nameof(currentUser)} not found");
        }

        if (group.StartDate > group.EndDate)
        {
            return new Result<bool>(false, $"Start date must be less than end date");
        }

        SetGroupStatus(group);
        
        try
        {
            await _repository.AddAsync(group);
            await _unitOfWork.Save();

            var addAdminsResult = await AddAllAdminsAtGroup(group);

            if (!addAdminsResult.IsSuccessful)
            {
                return new Result<bool>(false, $"{addAdminsResult.Message}");
            }

            if (currentUser.Role == AppUserRoles.Teacher)
            {
                var userGroup = new UserGroups()
                {
                    Group = group,
                    AppUser = currentUser
                };

                var createUserGroupResult = await CreateUserGroup(userGroup);

                if (!createUserGroupResult.IsSuccessful)
                {
                    return new Result<bool>(false, $"{createUserGroupResult.Message}");
                }
            }

            return new Result<bool>(true);
        }
        catch (Exception ex)
        {
            return new Result<bool>(false, $"Failed to create group {group.Name}. Exception: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteGroup(int groupId)
    {
        var group = await _repository.GetByIdAsync(groupId);

        if (group == null)
        {
            return new Result<bool>(false, $"Group by id {groupId} not found");
        }

        foreach (var notification in group.Notifications)
        {
            notification.AssignmentId = null;
            notification.GroupId = null;
            notification.CourseId = null;
        }

        try
        {
            if (group.EducationMaterials.Any())
            {
                var educationMaterialsCopy = group.EducationMaterials.ToList();

                foreach (var material in educationMaterialsCopy)
                {
                    await _educationMaterial.DeleteFile(material);
                }
            }

            await _repository.DeleteAsync(group);
            await _unitOfWork.Save();

            return new Result<bool>(true);
        }
        catch (Exception ex)
        {
            return new Result<bool>(false, $"Failed to delete group by id {groupId}. Exception: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteUserFromGroup(Group group, AppUser deletedUser)
    {
        if (group == null)
        {
            return new Result<bool>(false, $"{nameof(group)} not found");
        }

        if (deletedUser == null)
        {
            return new Result<bool>(false, $"{nameof(deletedUser)} not found");
        }
        
        var delUserGroup = new UserGroups()
        {
            AppUserId = deletedUser.Id,
            GroupId = group.Id
        };
        
        var userGroups = group.UserGroups;
        
        try
        {
            foreach (var userGroup in userGroups)
            {
                if (userGroup.AppUserId == delUserGroup.AppUserId && userGroup.GroupId == delUserGroup.GroupId)
                {
                    userGroups.Remove(userGroup);

                    break;
                }
            }
           
            group.UserGroups = userGroups;
            
            await _unitOfWork.Save();

            return new Result<bool>(true);
        }
        catch (Exception ex)
        {
            return new Result<bool>(false, $"Failed to delete group {group.Name}. Exception: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateGroup(Group newGroup)
    {
        if (newGroup == null)
        {
            return new Result<bool>(false, $"{nameof(newGroup)} not found");
        }

        if (newGroup.StartDate > newGroup.EndDate)
        {
            return new Result<bool>(false, $"Start date must be less than end date");
        }

        try
        {
            var group = await _repository.GetByIdAsync(newGroup.Id);
            group.Name = newGroup.Name;
            group.StartDate = newGroup.StartDate;
            group.EndDate = newGroup.EndDate;

            await _repository.UpdateAsync(group);
            await _unitOfWork.Save();

            return new Result<bool>(true);
        }
        catch (Exception ex)
        {
            return new Result<bool>(false, $"Failed to update group {newGroup.Id}. Exception: {ex.Message}");
        }
    }

    public async Task<string> CalculateStudentProgressInGroup(Group group, AppUser currentUser)
    {
        var totalAssignments = group.Assignments.Count;

        int completedAssignments = group.Assignments
            .SelectMany(assignment => assignment.UserAssignments)
            .Count(userAssignment => userAssignment.AppUserId == currentUser.Id && userAssignment.IsChecked);

        if (completedAssignments == 0)
        {
            return "0.0";
        }

        var averageProgress = (int)((double)completedAssignments / totalAssignments * 100);
        var strAverageProgress = $"{averageProgress:0.##}";

        return strAverageProgress;
    }

    public async Task<string> CalculateGroupProgress(int groupId)
    {
        var group = await _repository.GetByIdAsync(groupId);

        if (group == null || group.Assignments == null || group.Assignments.Count == 0)
        {
            return "0.0";
        }

        var totalAssignments = group.Assignments.Count;
        var allStudents = (group.UserGroups.Where(ug => ug.AppUser.Role == AppUserRoles.Student)).Count();

        var completedAssignments = group.Assignments.Sum(a => a.UserAssignments.Count(ua => ua.Grade > 0));
        var groupProgress = (double)completedAssignments / (totalAssignments * allStudents) * 100;
        
        var strGroupProgress = $"{groupProgress:0.##}";
        
        return strGroupProgress;
    }

    public async Task<Result<List<Group>>> GetAllGroupsAsync()
    {
        var groups = await _repository.GetAllAsync();

        if (!groups.Any())
        {
            return new Result<List<Group>>(false, "Group list is empty");
        }

        return new Result<List<Group>>(true, groups);
    }

    public async Task<Result<List<Group>>> GetUserGroups(AppUser currentUser, SortingParam sortOrder, string groupAccessFilter = null, string searchQuery = null)
    {
        Result<List<Group>> groupResult = null;

        var groups = currentUser.UserGroups.Select(ug => ug.Group).ToList();

        if (!groups.Any())
        {
            return new Result<List<Group>>(true, new List<Group>());
        }

        var query = GetOrderByExpression(sortOrder);

        if (!string.IsNullOrEmpty(searchQuery) && !string.IsNullOrEmpty(groupAccessFilter))
        {
            var tempFilter = Enum.Parse(typeof(GroupAccess), groupAccessFilter);
            groupResult = await GetByPredicate(g => groups.Contains(g) && g.Name.Contains(searchQuery) && g.GroupAccess.Equals(tempFilter), query);
        }
        else if (!string.IsNullOrEmpty(searchQuery))
        {
            groupResult = await GetByPredicate(g => groups.Contains(g) && g.Name.Contains(searchQuery), query);
        }
        else if (groupAccessFilter != null)
        {
            var tempFilter = Enum.Parse(typeof(GroupAccess), groupAccessFilter);
            groupResult = await GetByPredicate(g => groups.Contains(g) && g.GroupAccess.Equals(tempFilter), query);
        }
        else
        {
            groupResult = await GetByPredicate(g => groups.Contains(g), query);
        }

        if (!groupResult.IsSuccessful)
        {
            return new Result<List<Group>>(false, $"{groupResult.Message}");
        }
        
        var userGroups = await CheckStartAndEndGroupDate(groupResult.Data);

        return new Result<List<Group>>(true, userGroups);
    }

    private async Task<Result<bool>> AddAllAdminsAtGroup(Group group)
    {
        if (group.Course.UserCourses.Any())
        {
            foreach (var userCourse in group.Course.UserCourses)
            {
                if (userCourse.AppUser.Role == AppUserRoles.Admin)
                {
                    var userGroup = new UserGroups()
                    {
                        Group = group,
                        AppUser = userCourse.AppUser
                    };

                    var createUserGroupResult = await CreateUserGroup(userGroup);

                    if (!createUserGroupResult.IsSuccessful)
                    {
                        return new Result<bool>(false, $"{createUserGroupResult.Message}");
                    }
                }
            }
        }

        return new Result<bool>(true);
    }

    private async Task<Result<bool>> CreateUserGroup(UserGroups userGroups)
    {
        var createUserGroupResult = await _userGroupService.CreateUserGroups(userGroups);

        if (!createUserGroupResult.IsSuccessful)
        {
            await _repository.DeleteAsync(userGroups.Group);
            await _unitOfWork.Save();

            return new Result<bool>(false, $"{createUserGroupResult.Message}");
        }

        return new Result<bool>(true);
    }
    
    private async Task<Result<bool>> SetStudentProgressInUserGroup(Group group, AppUser currentUser, double progress)
    {
        if (group == null)
        {
            return new Result<bool>(false, $"{nameof(group)} not found");
        }
        
        if (currentUser == null)
        {
            return new Result<bool>(false, $"{nameof(currentUser)} not found");
        }
    
        var userGroups = await _userGroupService.GetByPredicate(ug => ug.AppUserId == currentUser.Id && ug.GroupId == group.Id);
        var userGroup = userGroups.Data.FirstOrDefault();

        var updateProgressResult = await _userGroupService.UpdateProgressInUserGroups(userGroup, progress);
    
        if (!updateProgressResult.IsSuccessful)
        {
            return new Result<bool>(false, $"{updateProgressResult.Message}");
        }
    
        return new Result<bool>(true);
    }
    
    private async Task<List<Group>> CheckStartAndEndGroupDate(List<Group> groups)
    {
        foreach (var group in groups)
        {
            SetGroupStatus(group);

            await _repository.UpdateAsync(group);
        }

        await _unitOfWork.Save();

        return groups;
    }
    
    private void SetGroupStatus(Group group)
    {
        if (group.StartDate > DateTime.Now)
        {
            group.GroupAccess = GroupAccess.Planned;
        }

        if (group.StartDate <= DateTime.Now)
        {
            group.GroupAccess = GroupAccess.InProgress;
        }

        if (group.EndDate < DateTime.Now)
        {
            group.GroupAccess = GroupAccess.Completed;
        }
    }
    
    private Expression<Func<IQueryable<Group>, IOrderedQueryable<Group>>> GetOrderByExpression(SortingParam sortBy)
    {
        Expression<Func<IQueryable<Group>, IOrderedQueryable<Group>>> query;

        switch (sortBy)
        {
            case SortingParam.NameDesc:
                query = q => q.OrderByDescending(q => q.Name);
                break;
            case SortingParam.StartDateDesc:
                query = q => q.OrderByDescending(q => q.StartDate);
                break;
            case SortingParam.StartDate:
                query = q => q.OrderBy(q => q.StartDate);
                break;
            case SortingParam.EndDate:
                query = q => q.OrderBy(q => q.EndDate);
                break;
            case SortingParam.EndDateDesc:
                query = q => q.OrderByDescending(q => q.EndDate);
                break;
            default:
                query = q => q.OrderBy(q => q.Name);
                break;
        }

        return query;
    }
}