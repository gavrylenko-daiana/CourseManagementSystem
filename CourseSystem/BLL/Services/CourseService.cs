using BLL.Interfaces;
using Core.Models;
using DAL.Interfaces;
using DAL.Repository;
using Microsoft.AspNetCore.Identity;

namespace BLL.Services;

public class CourseService : GenericService<Course>, ICourseService
{
    private readonly IUserCourseService _userCourseService;
    
    public CourseService(UnitOfWork unitOfWork, IUserCourseService userCourseService) 
        : base(unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.CourseRepository;
        _userCourseService = userCourseService;
    }

    public async Task CreateCourse(Course course, AppUser currentUser)
    {
        try
        {
            if (string.IsNullOrEmpty(course.Name))
            {
                throw new Exception("Course title cannot be empty.");
            }

            await Add(course);
            await _unitOfWork.Save();
            
            var userCourse = new UserCourses()
            {
                Course = course,
                CourseId = course.Id,
                AppUser = currentUser,
                AppUserId = currentUser.Id
            };
            await _userCourseService.CreateUserCourses(userCourse);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create course {course.Name}. Exception: {ex.Message}");
        }
    }

    public async Task DeleteCourse(int courseId)
    {
        try
        {
            var course = await GetById(courseId);
            
            if (course == null)
            {
                throw new Exception("Course not found");
            }

            await Delete(course.Id);
            await _unitOfWork.Save();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete course. Exception: {ex.Message}");
        }
    }

    public async Task UpdateName(int courseId, string newName)
    {
        try
        {
            var course = await GetById(courseId);
            
            if (course == null)
            {
                throw new Exception("Course not found");
            }

            course.Name = newName;

            await Update(course);
            await _unitOfWork.Save();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update course by {courseId} with new name: {newName}. Exception: {ex.Message}");
        }
    }
}