﻿using BLL.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Core.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace UI.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly INotificationService _notificationService;

    public NotificationController(UserManager<AppUser> userManager, INotificationService notificationService)
    {
        _userManager = userManager;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> ViewNew()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        var notifications = currentUser.Notifications.NotRead().OrderByDate();

        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> ViewAll()
    {
        var currentUser = await _userManager.GetUserAsync(User);

        var notifications = currentUser.Notifications.OrderByDate();

        return View(notifications);
    }

    [HttpGet]
    public async Task<IActionResult> NotificationDetails(int id)
    {
        var notification = await _notificationService.GetById(id);

        await _notificationService.MarkAsRead(notification);

        if (notification == null)
        {
            TempData.TempDataMessage("Error", "This notification does not exist.");

            // edit path
            return RedirectToAction("Index", "Home");
        }

        return View(notification);
    }
}