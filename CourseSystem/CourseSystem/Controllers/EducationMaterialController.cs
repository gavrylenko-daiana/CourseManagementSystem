using System.Text.Json;
using System.Text.Json.Serialization;
using BLL.Interfaces;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Dropbox.Api.Files;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using UI.ViewModels;
using static Dropbox.Api.Team.GroupSelector; //I don't know what that is, need to check
using UI.ViewModels.FileViewModels;
using BLL.Services;

namespace UI.Controllers;

public class EducationMaterialController : Controller
{
    private readonly IEducationMaterialService _educationMaterialService;
    private readonly IGroupService _groupService;
    private readonly ICourseService _courseService;
    private readonly IActivityService _activityService;
    private readonly IUserService _userService;
    private readonly ILogger<EducationMaterialController> _logger;

    public EducationMaterialController(IEducationMaterialService educationMaterial, IGroupService groupService,
        ICourseService courseService, IActivityService activityService,
        IUserService userService, ILogger<EducationMaterialController> logger)
    {
        _educationMaterialService = educationMaterial;
        _groupService = groupService;
        _courseService = courseService;
        _activityService = activityService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> IndexAccess(MaterialAccess access)
    {
        var materials = await _educationMaterialService.GetAllMaterialByAccessAsync(access);

        if (!(materials.IsSuccessful && materials.Data.Any()))
        {
            _logger.LogError("Failed to retrieve educational materials! Error: {errorMessage}", materials.Message);

            TempData.TempDataMessage("Error", $"Message: {materials.Message}");
            return RedirectToAction("Index", "Course");
        }

        return View("Index", materials.Data);
    }

    [HttpGet]
    public async Task<IActionResult> IndexMaterials(string materialIds)
    {
        var materialsList = await _educationMaterialService.GetMaterialsListFromIdsString(materialIds);
        
        if (!materialsList.IsSuccessful)
        {
            TempData.TempDataMessage("Error", materialsList.Message);
            return RedirectToAction("Index", "Course");
        }

        return View("Index", materialsList.Data);
    }
    
    [HttpGet]
    public async Task<IActionResult> IndexSort(List<EducationMaterial> materials, string sortBy)
    {
        if (!(materials != null && materials.Any()))
        {
            TempData.TempDataMessage("Error", $"Message: {nameof(materials)} list is empty");
            return RedirectToAction("Index", "Course");
        }

        return View("Index", materials);
    }

    [HttpGet]
    public async Task<IActionResult> CreateInGroup(int groupId)
    {
        var groupResult = await _groupService.GetById(groupId);

        if (!groupResult.IsSuccessful)
        {
            _logger.LogError("Failed to get group by Id {groupId}! Error: {errorMessage}",
                groupId, groupResult.Message);

            TempData.TempDataMessage("Error", $"Message - {groupResult.Message}");
            return RedirectToAction("Index", "Group");
        }

        var materialViewModel = new CreateInGroupEducationMaterialViewModel
        {
            Group = groupResult.Data,
            GroupId = groupResult.Data.Id,
            Course = groupResult.Data.Course,
            CourseId = groupResult.Data.CourseId
        };

        return View(materialViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInGroup(CreateInGroupEducationMaterialViewModel viewModel)
    {
        var currentUserResult = await _userService.GetCurrentUser(User);

        if (!currentUserResult.IsSuccessful)
        {
            _logger.LogWarning("Unauthorized user");

            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogError("Failed to create eduactional material! Invalid model state.");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogError("Error: {errorMessage}", error.ErrorMessage);
            }

            TempData.TempDataMessage("Error", "Incorrect data. Please try again!");

            return View(viewModel);
        }

        var groupResult = await _groupService.GetById(viewModel.GroupId);

        if (!groupResult.IsSuccessful)
        {
            _logger.LogError("Failed to get group by Id {groupId}! Error: {errorMessage}",
                viewModel.GroupId, groupResult.Message);

            TempData.TempDataMessage("Error", $"Message: {groupResult.Message}");
            return RedirectToAction("CreateInGroup", "EducationMaterial", new { groupId = viewModel.GroupId });
        }

        var addResult = await _courseService.AddEducationMaterial(viewModel.TimeUploaded, viewModel.UploadFile, viewModel.MaterialAccess, viewModel.GroupId, viewModel.CourseId);

        if (!addResult.IsSuccessful)
        {
            _logger.LogError("Failed to upload educational material! Error: {errorMessage}", addResult.Message);

            TempData.TempDataMessage("Error", $"Message: {addResult.Message}");
            return RedirectToAction("CreateInGroup", "EducationMaterial", new { groupId = viewModel.GroupId });
        }

        await _activityService.AddAttachedEducationalMaterialForGroupActivity(currentUserResult.Data, groupResult.Data);

        return RedirectToAction("Details", "Group", new { id = viewModel.GroupId });
    }

    [HttpGet]
    public async Task<IActionResult> CreateInCourse(int courseId)
    {
        var courseResult = await _courseService.GetById(courseId);

        if (!courseResult.IsSuccessful)
        {
            _logger.LogError("Failed to get course by Id {courseId}! Error: {errorMessage}",
                courseId, courseResult.Message);

            TempData.TempDataMessage("Error", $"Message - {courseResult.Message}");
            return RedirectToAction("Index", "Course");
        }

        var materialViewModel = new CreateInCourseEducationMaterialViewModel
        {
            Course = courseResult.Data,
            CourseId = courseResult.Data.Id,
            Groups = courseResult.Data.Groups
        };

        return View(materialViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInCourse(CreateInCourseEducationMaterialViewModel viewModel)
    {
        var currentUserResult = await _userService.GetCurrentUser(User);

        if (!currentUserResult.IsSuccessful)
        {
            _logger.LogWarning("Unauthorized user");

            return RedirectToAction("Login", "Account");
        }

        var courseResult = await _courseService.GetById(viewModel.CourseId);

        if (!courseResult.IsSuccessful)
        {
            _logger.LogError("Failed to get course by Id {courseId}! Error: {errorMessage}",
                viewModel.CourseId, courseResult.Message);

            TempData.TempDataMessage("Error", $"Message: {courseResult.Message}");
            return RedirectToAction("CreateInCourse", "EducationMaterial", new { courseId = viewModel.CourseId });
        }

        var addResult = await _courseService.AddEducationMaterial(viewModel.TimeUploaded, viewModel.UploadFile, viewModel.MaterialAccess,
            viewModel.SelectedGroupId, viewModel.CourseId);

        if (!addResult.IsSuccessful)
        {
            _logger.LogError("Failed to upload educational material! Error: {errorMessage}", addResult.Message);

            TempData.TempDataMessage("Error", $"Message: {addResult.Message}");
            return RedirectToAction("CreateInCourse", "EducationMaterial", new { courseId = viewModel.CourseId });
        }

        await _activityService.AddAttachedEducationalMaterialForCourseActivity(currentUserResult.Data, courseResult.Data);

        return RedirectToAction("Details", "Course", new { id = viewModel.CourseId });
    }

    [HttpGet]
    public async Task<IActionResult> CreateInGeneral()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        var groups = await _groupService.GetAllGroupsAsync();

        var materialViewModel = new CreateInGeneralEducationMaterialViewModel
        {
            Courses = courses.Data!,
            Groups = groups.Data!
        };
        
        return View(materialViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInGeneral(CreateInGeneralEducationMaterialViewModel viewModel)
    {
        var currentUserResult = await _userService.GetCurrentUser(User);

        if (!currentUserResult.IsSuccessful)
        {
            _logger.LogWarning("Unauthorized user");

            return RedirectToAction("Login", "Account");
        }

        var addResult = await _courseService.AddEducationMaterial(viewModel.TimeUploaded, viewModel.UploadFile, viewModel.MaterialAccess,
            viewModel.SelectedGroupId, viewModel.SelectedCourseId);

        if (!addResult.IsSuccessful)
        {
            _logger.LogError("Failed to upload educational material! Error: {errorMessage}", addResult.Message);

            TempData.TempDataMessage("Error", $"Message: {addResult.Message}");
            return RedirectToAction("CreateInGeneral", "EducationMaterial");
        }

        await _activityService.AddAttachedGeneralEducationalMaterialActivity(currentUserResult.Data);

        return RedirectToAction("Index", "Course");
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var material = await _educationMaterialService.GetByIdMaterialAsync(id);

        if (!material.IsSuccessful)
        {
            _logger.LogError("Failed to get educational material by Id {materialId}! Error: {errorMessage}",
                id, material.Message);

            TempData.TempDataMessage("Error", $"Message: {material.Message}");

            return RedirectToAction("Index", "Course");
        }

        TempData["UploadResult"] = material.Data.Url;

        return View(material.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var fileToDelete = await _educationMaterialService.GetByIdMaterialAsync(id);

        if (!fileToDelete.IsSuccessful)
        {
            _logger.LogError("Failed to get educational material by Id {materialId}! Error: {errorMessage}",
                id, fileToDelete.Message);

            TempData.TempDataMessage("Error", $"Message: {fileToDelete.Message}");

            return RedirectToAction("Detail", "EducationMaterial", new { id = id });
        }

        var deleteResult = await _educationMaterialService.DeleteFile(fileToDelete.Data);

        if (!deleteResult.IsSuccessful)
        {
            _logger.LogError("Failed to delete educational material by Id {materialId}! Error: {errorMessage}",
                fileToDelete.Data.Id, deleteResult.Message);

            TempData.TempDataMessage("Error", $"Message: {deleteResult.Message}");

            return RedirectToAction("Detail", "EducationMaterial", new { id = id });
        }

        // var updateResult = await _groupService.UpdateGroup(deleteResult.Data);

        // if (!updateResult.IsSuccessful)
        // {
        //     _logger.LogError("Failed to update group by Id {groupId}! Error: {errorMessage}",
        //         deleteResult.Data.Id, updateResult.Message);
        //     TempData.TempDataMessage("Error", $"Message: {updateResult.Message}");
        // }

        return RedirectToAction("Index", "Course");
    }
}