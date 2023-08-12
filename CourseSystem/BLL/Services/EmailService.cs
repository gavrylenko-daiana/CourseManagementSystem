﻿using BLL.Interfaces;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using Microsoft.AspNetCore.Identity;
using Core.Models;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using Core.Configuration;
using MailKit.Security;
using System.Runtime;
using Core.Enums;
using Core.EmailTemplates;
using Microsoft.IdentityModel.Tokens;

namespace BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailSettings _emailSettings;


        public EmailService(UserManager<AppUser> userManager,
            IOptions<EmailSettings> settings)
        {
            _userManager = userManager;
            _emailSettings = settings.Value;
        }
              
        public async Task<int> SendCodeToUser(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) 
                throw new ArgumentNullException(nameof(email));

            int randomCode = new Random().Next(1000, 9999);

            try
            {
                var subjectAndBody = EmailTemplate.GetEmailSubjectAndBody(EmailType.CodeVerification, 
                    new Dictionary<string, object>() { { @"{randomcode}", randomCode } });
                
                var emailData = new EmailData(
                    new List<string>() { email},
                    subjectAndBody.Item1,
                    subjectAndBody.Item2
                    );

                var result = await SendEmailAsync(emailData);

                if(!result.IsSuccessful)
                {
                    return 0;
                }

                return randomCode;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task<Result<bool>> SendEmailAsync(EmailData emailData)
        {
            try
            {
                var emailMessage = new MimeMessage();

                emailMessage.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.From));

                foreach (string emailToAdress in emailData.To)
                    emailMessage.To.Add(MailboxAddress.Parse(emailToAdress));

                #region Content

                emailMessage.Subject = emailData.Subject;
                emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = emailData.Body
                };
                #endregion

                #region Email sending

                using (var client = new SmtpClient())
                {
                    if (_emailSettings.UseSSL)
                    {
                        await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.SslOnConnect);
                    }
                    else if (_emailSettings.UseStartTls)
                    {
                        await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
                    }

                    await client.AuthenticateAsync(_emailSettings.UserName, _emailSettings.Password); //ключ доступа от Гугл
                    await client.SendAsync(emailMessage);

                    await client.DisconnectAsync(true);
                }
                #endregion

                return new Result<bool>(true);
            }
            catch (Exception ex)
            {
                return new Result<bool>(false, "Fail to send email");
            }
        }

        public async Task<Result<bool>> SendToTeacherCourseInventation(AppUser teacher, Course course, string inventationUrl)
        {
            if (teacher == null || course == null)
                return new Result<bool>(false, $"Fail to send email inventation to the techer");

            var emailContent = GetEmailSubjectAndBody(EmailType.CourseInvitation, teacher, course, inventationUrl);

            return await CreateAndSendEmail(new List<string> { teacher.Email }, emailContent.Item1, emailContent.Item2);
        }

        public async Task<Result<bool>> SendInventationToStudents(Dictionary<string, string> studentsData, Group group)
        {
            try
            {
                foreach(var studentData in studentsData)
                {
                    var emailContent = GetEmailSubjectAndBody(EmailType.GroupInvitationToStudent, group, studentData.Value, await _userManager.FindByEmailAsync(studentData.Key));
                    var result = await CreateAndSendEmail(new List<string> { studentData.Key}, emailContent.Item1, emailContent.Item2);

                    if (!result.IsSuccessful)
                        return new Result<bool>(false, result.Message);
                }
               
                return new Result<bool>(true, "Emails were sent to students");
            }
            catch (Exception ex)
            {
                return new Result<bool>(false, "Fail to send email to students");
            }

        }

        private async Task<Result<bool>> CreateAndSendEmail(List<string> toEmails, string subject, string body = null, string displayName = null)
        {
            if (toEmails.IsNullOrEmpty())
                return new Result<bool>(false, "No emails to send data");

            var emailData = new EmailData(toEmails, subject, body, displayName);

            try
            {
                var result = await SendEmailAsync(emailData);

                if (!result.IsSuccessful)
                    return new Result<bool>(false, result.Message);

                return new Result<bool>(true);
            }
            catch (Exception ex)
            {
                return new Result<bool>(false, "Fail to send email");
            }

        }

        private (string, string) GetEmailSubjectAndBody(EmailType emailType, AppUser appUser,  string callBackUrl = null) 
        {
            if(appUser == null)
                return (String.Empty, String.Empty);

            var parameters = new Dictionary<string, object>();
            switch(emailType)
            {
                case EmailType.AccountApproveByAdmin:
                    parameters = new Dictionary<string, object>()
                    {
                        {@"{firstname}", appUser.FirstName },
                        {@"{lastname}", appUser.LastName },
                        {@"{email}", appUser.Email },
                        {@"{userrole}", appUser.Role},
                        {@"{callbackurl}", callBackUrl }
                    };
                    break;
                case EmailType.UserRegistration:
                    parameters = new Dictionary<string, object>()
                    {
                        {@"{firstname}", appUser.FirstName },
                        {@"{callbackurl}", callBackUrl }
                    };
                    break;
                case EmailType.ConfirmAdminRegistration:
                    parameters = new Dictionary<string, object>()
                    {
                        {@"{callbackurl}", callBackUrl }
                    };
                    break;
                case EmailType.ConfirmDeletionByAdmin:
                    parameters = new Dictionary<string, object>()
                    {
                        {@"{firstname}", appUser.FirstName },
                        {@"{lastname}", appUser.LastName },
                        {@"{email}", appUser.Email },
                        {@"{userrole}", appUser.Role},
                        {@"{callbackurl}", callBackUrl }
                    };
                    break;
                case EmailType.ConfirmDeletionByUser:
                    parameters = new Dictionary<string, object>()
                    {
                        {@"{firstname}", appUser.FirstName },
                        {@"{callbackurl}", callBackUrl }
                    };
                    break;
                case EmailType.AccountApproveByUser:
                    parameters = new Dictionary<string, object>()
                    {
                        {@"{firstname}", appUser.FirstName },
                        {@"{lastname}", appUser.LastName },
                        {@"{callbackurl}", callBackUrl }
                    };
                    break;
                default:
                    return (String.Empty, String.Empty);
            }

            return EmailTemplate.GetEmailSubjectAndBody(emailType, parameters);
        }

        private (string, string) GetEmailSubjectAndBody(EmailType emailType, Group group, string callBackUrl = null, AppUser appUser = null)
        {
            if (group == null)
                return (String.Empty, String.Empty);

            var parameters = new Dictionary<string, object>();
            var groupEmailTypes = new List<EmailType>() {
                EmailType.GroupConfirmationByAdmin,
                EmailType.ApprovedGroupCreation,
                EmailType.GroupInvitationToStudent
            };

            if (!groupEmailTypes.Contains(emailType))
            {
                return (String.Empty, String.Empty);
            }

            parameters = new Dictionary<string, object>()
                    {
                        {@"{groupname}", group.Name },
                        {@"{callbackurl}", callBackUrl }
                    };           

            return EmailTemplate.GetEmailSubjectAndBody(emailType, parameters);
        }

        private (string, string) GetEmailSubjectAndBody(EmailType emailType, AppUser appUser, Course course, string callBackUrl = null)
        {
            if (appUser == null || course == null)
                return (String.Empty, String.Empty);

            var parameters = new Dictionary<string, object>();
            switch (emailType)
            {
                case EmailType.CourseInvitation:
                    if (course == null)
                        break;

                    parameters = new Dictionary<string, object>()
                    {
                        {@"{firstname}", appUser.FirstName },
                        {@"{coursename}", course.Name },
                        {@"{callbackurl}", callBackUrl }
                    };
                    break;
                default:
                    return (String.Empty, String.Empty);
            }

            return EmailTemplate.GetEmailSubjectAndBody(emailType, parameters);
        }

        public async Task<Result<bool>> SendEmailToAppUsers(EmailType emailType, AppUser appUser, string callBackUrl = null)
        {
            if (appUser == null)
                return new Result<bool>(false, "Fail to send email");

            var emailContent = GetEmailSubjectAndBody(emailType, appUser, callBackUrl);

            var toEmail = new List<string>();
            var allAdmins = await _userManager.GetUsersInRoleAsync(AppUserRoles.Admin.ToString());
            
            switch (emailType)
            {               
                case EmailType.ConfirmDeletionByAdmin:                   
                    toEmail  = allAdmins.Select(a => a.Email).ToList();
                    break;
                case EmailType.AccountApproveByAdmin:                   
                    toEmail = allAdmins.Select(a => a.Email).ToList();
                    break;
                default:
                    toEmail.Add(appUser.Email);
                    break;
            }

            return await CreateAndSendEmail(toEmail, emailContent.Item1, emailContent.Item2);
        }

        public async Task<Result<bool>> SendEmailGroups(EmailType emailType,Group group, string callBackUrl = null, AppUser appUser = null)
        {
            if (group == null)
                return new Result<bool>(false, "Fail to send email");

            var emailContent = GetEmailSubjectAndBody(emailType, group, callBackUrl, appUser);

            var toEmail = new List<string>();
            var allAdmins = await _userManager.GetUsersInRoleAsync(AppUserRoles.Admin.ToString());

            switch (emailType)
            {
                case EmailType.GroupConfirmationByAdmin:
                    toEmail = allAdmins.Select(a => a.Email).ToList();
                    break;
                default:
                    toEmail.Add(appUser.Email);
                    break;
            }

            return await CreateAndSendEmail(toEmail, emailContent.Item1, emailContent.Item2);
        }

    }
}
