using BLL.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using UI.ViewModels;

namespace UI.Controllers;

public class CourseController : Controller
{
    private readonly ICourseService _courseService;

    public CourseController(ICourseService courseService)
    {
        _courseService = courseService;
    }
    
    public async Task<ViewResult> Index()
    {
        // all courses in db (change on user.courses)
        var courses = await _courseService.GetAll();
        var courseViewModels = courses.Select(c => new CourseViewModel
        {
            Name = c.Name
        }).ToList();

        return View(courseViewModels);
    }

}