﻿using Accio.Business.Models.RoleModels;
using System;
using System.Collections.Generic;

namespace Accio.Business.Models.AccountModels
{
    public class AccountModel
    {
        public Guid AccountId { get; set; }
        public string AccountName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PasswordHash { get; set; }
        public bool? Active { get; set; }
        public bool Verified { get; set; }
        public List<RoleModel> Roles { get; set; } = new List<RoleModel>();
        public Guid CreatedById { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid UpdatedById { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool Deleted { get; set; }
    }
}
