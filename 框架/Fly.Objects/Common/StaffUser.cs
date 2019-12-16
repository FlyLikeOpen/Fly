using System;
using System.Collections.Generic;

namespace Fly.Objects.Common
{
	public class StaffUser
	{
		public Guid Id { get; set; }

		//public long IdNumber { get; set; }

        public string LoginId { get; set; }

		public string DisplayName { get; set; }

		public string Email { get; set; }

		public string Mobile { get; set; }

        public string WorkWeChatUserId { get; set; }

        public DataStatus Status { get; set; }

		public DateTime CreatedOn { get; set; }

		public Guid CreatedBy { get; set; }

		public DateTime? UpdatedOn { get; set; }

		public Guid? UpdatedBy { get; set; }
	}
}