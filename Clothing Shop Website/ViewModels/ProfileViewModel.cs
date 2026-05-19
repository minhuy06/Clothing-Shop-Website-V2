using System;

namespace Clothing_Shop_Website.ViewModels
{
    public class ProfileViewModel
    {
        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public DateTime? DateOfBirth { get; set; }
        public int Gender { get; set; }
        public int RewardPoints { get; set; }
        public int Status { get; set; }

        // Hàm tạo Avatar 2 chữ cái đầu
        public string GetAvatarInitials()
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "NV";
            var word = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (word.Length == 1) return word[0].Substring(0, 1).ToUpper();
            return (word[0].Substring(0, 1) + word[^1].Substring(0, 1)).ToUpper();
        }
    }
}
