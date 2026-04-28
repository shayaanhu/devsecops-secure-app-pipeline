namespace CarpoolApp.Server.DTO
{
    public class LoginDto
    {
        public string UniversityEmail { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // "driver" or "passenger"
    }

}