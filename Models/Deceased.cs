using System;

namespace AreEyeP.Models
{
    public class Deceased
    {
        public int Id { get; set; }                // Primary Key
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime DateOfDeath { get; set; }
        public string Address { get; set; }
        public string CauseOfDeath { get; set; }
        public string Gender { get; set; }
        public int ApplicationId { get; set; }     // Foreign key linking back to BurialApplications
    }
}
