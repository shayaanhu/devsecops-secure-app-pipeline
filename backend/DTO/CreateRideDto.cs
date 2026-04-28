namespace CarpoolApp.Server.DTO
{
    public class CreateRideDto
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public List<string> RouteStops { get; set; }
        public DateTime DepartureTime { get; set; }
        public int VehicleId { get; set; }
        public int AvailableSeats { get; set; }
        public int PricePerSeat { get; set; }
    }

}