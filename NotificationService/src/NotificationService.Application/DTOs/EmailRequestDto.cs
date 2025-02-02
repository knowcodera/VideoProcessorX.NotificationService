using System.ComponentModel.DataAnnotations;

namespace NotificationService.Application.DTOs
{
    public class EmailRequestDto
    {
        [Required, EmailAddress]
        public string To { get; set; }

        [Required, StringLength(200)]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; }
    }
}
