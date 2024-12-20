using System;
using System.ComponentModel.DataAnnotations;

namespace AreEyeP.Models
{
    public class RegistrationViewModel
    {
        [Key]
        public int Id { get; set; }  // Primary Key (Auto-incremented in the database)

        [Required(ErrorMessage = "First Name is required")]
        [MaxLength(100, ErrorMessage = "First Name cannot exceed 100 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [MaxLength(100, ErrorMessage = "Last Name cannot exceed 100 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Contact Number is required")]
        [MaxLength(15, ErrorMessage = "Contact Number cannot exceed 15 characters")]
        public string ContactNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [MaxLength(255, ErrorMessage = "Username cannot exceed 255 characters")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MaxLength(255, ErrorMessage = "Password cannot exceed 255 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [MaxLength(10, ErrorMessage = "Gender cannot exceed 10 characters")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Birthdate is required")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Automatically set the CreatedDate

        // Foreign Key to Users Table
        public int UserId { get; set; }
    }
}
