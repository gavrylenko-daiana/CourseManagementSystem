using BLL.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UI.ViewModels;

namespace UI.Controllers;

public class CourseController : Controller
{
    private readonly ICourseService _courseService;
    private readonly UserManager<AppUser> _userManager;

    public CourseController(ICourseService courseService, UserManager<AppUser> userManager)
    {
        _courseService = courseService;
        _userManager = userManager;
    }
    
    public async Task<ViewResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null) return View("Error");
        
        var courses = await _courseService.GetByPredicate(course => 
            course.UserCourses.Any(uc => uc.AppUser.Id == currentUser.Id)
        );
        
        var courseViewModels = courses.Select(c => new CourseViewModel
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();

        return View(courseViewModels);
    }

    public ActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<ActionResult> Create(CourseViewModel courseViewModel)
    {
        try
        {
            var course = new Course
            {
                Name = courseViewModel.Name
            };

            await _courseService.CreateCourse(course);

            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ViewData["CreatingError"] = $"Failed to create course. Error: {e.Message}";
            return View(courseViewModel);
        }
    }
    
    public async Task<ActionResult> Edit(int id)
    {
        try
        {
            var course = await _courseService.GetById(id);
            if (course == null)
            {
                throw new Exception("Course not found");
            }

            var courseViewModel = new CourseViewModel
            {
                Name = course.Name
            };

            return View(courseViewModel);
        }
        catch (Exception e)
        {
            ViewData["EditingError"] = $"Failed to editing course. Error: {e.Message}";
            return View("Error");
        }
    }
    
    [HttpPost]
    public async Task<ActionResult> Edit(CourseViewModel courseViewModel)
    {
        try
        {
            var course = await _courseService.GetById(courseViewModel.Id);
            if (course == null)
            {
                throw new Exception("Course not found");
            }

            course.Name = courseViewModel.Name;

            await _courseService.Update(course);

            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ViewData["EditingError"] = $"Failed to editing course. Error: {e.Message}";
            return View(courseViewModel);
        }
    }
    
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var course = await _courseService.GetById(id);
            if (course == null)
            {
                throw new Exception("Course not found");
            }

            CourseViewModel courseViewModel = new CourseViewModel
            {
                Id = course.Id,
                Name = course.Name
            };

            return View(courseViewModel);
        }
        catch (Exception e)
        {
            ViewData["DeletingError"] = $"Failed to delete course. Error: {e.Message}";
            return View("Error");
        }
    }
    
    [HttpPost, ActionName("Delete")]
    public async Task<ActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var course = await _courseService.GetById(id);
            if (course == null)
            {
                throw new Exception("Course not found");
            }

            await _courseService.DeleteCourse(course.Id);

            return RedirectToAction("Index");
        }
        catch (Exception e)
        {
            ViewData["DeletingError"] = $"Failed to delete course. Error: {e.Message}";
            return View("Error");
        }
    }
}