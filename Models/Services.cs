using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ServiceName { get; set; }

        [Required]
        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Range(0.0, double.MaxValue)]
        public decimal MinPrice { get; set; }

        [Required]
        [Range(0.0, double.MaxValue)]
        public decimal MaxPrice { get; set; }
    }
}
