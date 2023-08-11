using BLL.Interfaces;
using Core.Enums;
using Core.Helpers;
using Core.Models;
using Dropbox.Api.Files;
using Microsoft.AspNetCore.Mvc;
using UI.ViewModels;

namespace UI.Controllers;

public class EducationMaterialController : Controller
{
    private readonly IEducationMaterialService _educationMaterialService;
    private readonly IGroupService _groupService;
    private readonly ICourseService _courseService;

    public EducationMaterialController(IEducationMaterialService educationMaterial, IGroupService groupService, ICourseService courseService)
    {
        _educationMaterialService = educationMaterial;
        _groupService = groupService;
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var materials = await _educationMaterialService.GetAllMaterialAsync();

        if (!(materials.IsSuccessful && materials.Data.Any()))
        {
            TempData.TempDataMessage("Error", $"Message: {materials.Message}");
            return RedirectToAction("Index", "Course");
        }

        return View(materials.Data);
    }

    [HttpGet]
    public async Task<IActionResult> CreateInGroup(int groupId)
    {
        // check result
        var group = await _groupService.GetById(groupId);

        var materialViewModel = new CreateInGroupEducationMaterialViewModel
        {
            Group = group,
            GroupId = group.Id
        };

        return View(materialViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInGroup(CreateInGroupEducationMaterialViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            TempData.TempDataMessage("Error", "Incorrect data. Please try again!");
            return View(viewModel);
        }

        var fullPath = await _educationMaterialService.AddFileAsync(viewModel.UploadFile);

        if (!fullPath.IsSuccessful)
        {
            TempData.TempDataMessage("Error", $"Message: {fullPath.Message}");
            return View(viewModel);
        }

        var addResult = await _educationMaterialService.AddToGroup(viewModel.UploadFile,
            fullPath.Message, viewModel.GroupId);

        if (!addResult.IsSuccessful)
        {
            TempData.TempDataMessage("Error", $"Message: {addResult.Message}");
            return View(viewModel);
        }

        return RedirectToAction("Details", "Group", new { id = viewModel.GroupId });
    }
    
    [HttpGet]
    public async Task<IActionResult> CreateInCourse(int courseId)
    {
        // check result
        var course = await _courseService.GetById(courseId);
    
        var materialViewModel = new CreateInCourseEducationMaterialViewModel
        {
            Course = course,
            CourseId = course.Id,
            Groups = course.Groups
        };
    
        return View(materialViewModel);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateInCourse(CreateInCourseEducationMaterialViewModel viewModel)
    {
        var fullPath = await _educationMaterialService.AddFileAsync(viewModel.UploadFile);
    
        if (!fullPath.IsSuccessful)
        {
            TempData.TempDataMessage("Error", $"Message: Failed to upload file");
            return View(viewModel);
        }

        if (viewModel.MaterialAccess == MaterialAccess.Group)
        {
            var addToGroupResult = await _educationMaterialService.AddToGroup(viewModel.UploadFile,
                fullPath.Message, viewModel.SelectedGroupId);
            
            if (!addToGroupResult.IsSuccessful)
            {
                TempData.TempDataMessage("Error", $"Message: {addToGroupResult.Message}");
                return View(viewModel);
            }
        }
        else
        {
            var addToCourseResult = await _educationMaterialService.AddToCourse(viewModel.UploadFile,
                fullPath.Message, viewModel.CourseId);
            
            if (!addToCourseResult.IsSuccessful)
            {
                TempData.TempDataMessage("Error", $"Message: {addToCourseResult.Message}");
                return View(viewModel);
            }
        }

        return RedirectToAction("Details", "Course", new { id = viewModel.CourseId });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var material = await _educationMaterialService.GetByIdMaterialAsync(id);

        TempData["UploadResult"] = material.Url;

        return View(material);
    }
    
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var fileToDelete = await _educationMaterialService.GetByIdMaterialAsync(id);

        if (fileToDelete == null)
        {
            return NotFound();
        }

        var access = fileToDelete.MaterialAccess;

        switch (access)
        {
            case MaterialAccess.Group:
                // await _educationMaterialService.DeleteGroup(fileToDelete);
                await _educationMaterialService.DeleteFileAsync(fileToDelete.Name);
                await _educationMaterialService.DeleteUploadFileAsync(fileToDelete);
            
                var group = fileToDelete.Group;

                if (group != null)
                {
                    group.EducationMaterials.Remove(fileToDelete);
                    await _groupService.UpdateGroup(group);
                }
                break;
            case MaterialAccess.Course:
                break;
            case MaterialAccess.General:
                break;
            default:
                break;
        }

        return RedirectToAction("Index", "Course");
    }
}