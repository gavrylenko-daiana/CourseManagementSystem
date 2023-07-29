﻿using BLL.Interfaces;
using Core.Enums;
using Core.Models;
using DAL.Repository;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class AssignmentService : GenericService<Assignment>, IAssignmentService
    {
        public AssignmentService(UnitOfWork unitOfWork) : base(unitOfWork)
        {
            _repository = unitOfWork.AssignmentRepository;
        }

        public async Task<Result<bool>> CreateAssignment(Assignment assignment)
        {
            if (assignment == null)
                return new Result<bool>(false, "Invalid assignment data");

            try
            {
                await _repository.AddAsync(assignment);
                await _unitOfWork.Save();

                return new Result<bool>(true);
            }
            catch (Exception ex)
            {
                return new Result<bool>(false, "Fail to save assignment");
            }
        }

        public async Task<Result<bool>> DeleteAssignment(int assignmentId)
        {
            var assignment = await GetById(assignmentId);

            if (assignment == null)
                return new Result<bool>(false, "Fail to get assignmnet");

            try
            {
                await _repository.DeleteAsync(assignment);
                await _unitOfWork.Save();

                return new Result<bool>(true);
            }
            catch(Exception ex)
            {
                return new Result<bool>(false, "Fail to delete assignment");
            }
        }

        public async Task<Result<List<Assignment>>> GetGroupAssignments(int groupId)
        {
            var group = await _unitOfWork.GroupRepository.GetByIdAsync(groupId); // group service

            if (group == null)
                return new Result<List<Assignment>>(false, "Failt to get group");

            if (group.Assignments.IsNullOrEmpty())
                return new Result<List<Assignment>>(true, "No assignment in group");

            var groupAssignments = await ChechStartAndEndAssignmnetDate(group.Assignments);

            return new Result<List<Assignment>>(true, groupAssignments);
        }

        public async Task<Result<bool>> UpdateAssignment(Assignment assignment)
        {
            if (assignment == null)
                return new Result<bool>(false, "Invalid assignment data");

            try
            {
                await _repository.UpdateAsync(assignment);
                await _unitOfWork.Save();

                return new Result<bool>(true);
            }
            catch (Exception ex)
            {
                return new Result<bool>(false, "Fail to update assgnment");
            }
        }

        private async Task<List<Assignment>> ChechStartAndEndAssignmnetDate(List<Assignment> assignments)
        {
            foreach(var assignment in assignments)
            {
                if(assignment.StartDate <= DateTime.Now)
                {
                    assignment.AssignmentAccess = AssignmentAccess.InProgress;
                }

                if(assignment.EndDate >= DateTime.Now) 
                {
                    assignment.AssignmentAccess = AssignmentAccess.AwaitingApproval;
                }

                await _repository.UpdateAsync(assignment);
            }

            await _unitOfWork.Save();

            return assignments;
        }
    }
}