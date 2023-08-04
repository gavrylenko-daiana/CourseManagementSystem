﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Notification : BaseEntity
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime Created { get; set; }

        public bool IsRead { get; set; }

        public string AppUserId { get; set; }
        public virtual AppUser AppUser { get; set; }
    }
}