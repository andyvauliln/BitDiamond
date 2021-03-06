﻿using Axis.Pollux.Identity.Principal;
using System.ComponentModel.DataAnnotations;

namespace BitDiamond.Core.Models
{
    public class Notification: BaseModel<long>
    {
        [MaxLength(500, ErrorMessage = "Title is too long")]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public NotificationType Type { get; set; }
        public bool Seen { get; set; }
        
        [MaxLength(400, ErrorMessage = "Context is too long")]
        public string Context { get; set; }
        [MaxLength(400, ErrorMessage = "Context id is too long")]
        public string ContextId { get; set; }

        private User _target;
        private string _targetId;
        public virtual User Target
        {
            get { return _target; }
            set
            {
                _target = value;
                if (value != null) _targetId = _target.EntityId;
                else _targetId = null;
            }
        }
        [Required(ErrorMessage = "Target id is Required")]
        public string TargetId
        {
            get { return _targetId; }
            set
            {
                _targetId = value;
                if (value == null) _target = null;
                else if (!value.Equals(_target?.EntityId)) _target = null;
            }
        }
    }

    public enum NotificationType
    {
        Info,
        Error,
        Warning,
        Success
    }
}
