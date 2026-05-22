using System;
using System.Collections.Generic;
using Clothing_Shop_Website.Models;

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

        // Tạo List chứa danh sách địa chỉ
        public List<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

    }
}
