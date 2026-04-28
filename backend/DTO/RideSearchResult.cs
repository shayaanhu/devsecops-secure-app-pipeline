namespace CarpoolApp.Server.DTO
{
    public class RideSearchResultDto
    {
        public int RideId { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string DepartureTime { get; set; }
        public int AvailableSeats { get; set; }
        public decimal PricePerSeat { get; set; }
        public string DriverName { get; set; }
        public string VehicleModel { get; set; }
        public List<string> RouteStops { get; set; } = new List<string>();
        public string RideRequestStatus { get; set; } = "Not Requested";
    }
}