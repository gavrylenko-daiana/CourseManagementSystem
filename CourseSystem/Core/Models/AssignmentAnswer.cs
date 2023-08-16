﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class AssignmentAnswer : BaseEntity
    {
        public string Name { get; set; }
        public string? Text { get; set; }
        public DateTime CreationTime { get; set; }
        public string Url { get; set; }
        public int UserAssignmentId { get; set; }
        public virtual UserAssignments UserAssignment { get; set; }
    }
}