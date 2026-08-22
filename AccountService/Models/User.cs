namespace AccountService.Models
{
    public class User
    {
        // ❌ KHÔNG ĐỂ: public int Id { get; set; }

        // ✅ PHẢI ĐỔI THÀNH GUID CHO KHỚP VỚI ĐỊNH DẠNG UUID CỦA NEON DB:
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
