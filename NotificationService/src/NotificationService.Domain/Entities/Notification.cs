namespace NotificationService.Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Sent { get; set; }
        public DateTime? SentAt { get; set; }
        public int Attempts { get; set; } // Novo campo
        public string LastError { get; set; }

    }
}
