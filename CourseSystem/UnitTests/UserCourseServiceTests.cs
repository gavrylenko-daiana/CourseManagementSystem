using BLL.Interfaces;
using BLL.Services;
using Core.Enums;
using Core.Models;
using DAL;
using DAL.Interfaces;
using DAL.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests;

public class UserCourseServiceTests
{
    [Fact]
    public async Task CreateUserCourses_ValidUserCourses_ReturnsTrue()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: "test_database")
            .Options;
        
        using (var context = new ApplicationContext(options))
        {
            var unitOfWork = new UnitOfWork(context);
            var userGroupServiceMock = new Mock<IUserGroupService>();

            var userCourseService = new UserCourseService(unitOfWork, userGroupServiceMock.Object);
        
            var userCourses = new UserCourses
            {
                AppUserId = "user123",
                CourseId = 1
            }; 
        
            var result = await userCourseService.CreateUserCourses(userCourses);
        
            Assert.True(result.IsSuccessful);
        }
    }
    
    [Theory]
    [InlineData(null, 1)]
    public async Task CreateUserCourses_InvalidUserCourses_ReturnsFalse(string appUserId, int courseId)
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: "test_database")
            .Options;
        
        using (var context = new ApplicationContext(options))
        {
            var unitOfWork = new UnitOfWork(context);
            var userGroupServiceMock = new Mock<IUserGroupService>();

            var userCourseService = new UserCourseService(unitOfWork, userGroupServiceMock.Object);
        
            var userCourses = new UserCourses
            {
                AppUserId = "user123",
                CourseId = 1
            }; 
        
            var result = await userCourseService.CreateUserCourses(userCourses);
        
            Assert.False(result.IsSuccessful);
        }
    }
    
    [Fact]
    public async Task CreateUserCourses_NullUserCourses_ReturnsFalse()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: "test_database")
            .Options;
        
        using (var context = new ApplicationContext(options))
        {
            var unitOfWork = new UnitOfWork(context);
            var userGroupServiceMock = new Mock<IUserGroupService>();

            var userCourseService = new UserCourseService(unitOfWork, userGroupServiceMock.Object);
        
            UserCourses invalidUserCourses = null;
            
            var result = await userCourseService.CreateUserCourses(invalidUserCourses);
        
            Assert.False(result.IsSuccessful);
            Assert.Equal($"userCourses not found", result.Message);
        }
    }
}