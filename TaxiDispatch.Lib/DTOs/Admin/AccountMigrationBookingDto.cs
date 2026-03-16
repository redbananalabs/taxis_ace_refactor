namespace TaxiDispatch.DTOs.Admin;

public class AccountMigrationBookingDto
{
    public int Id { get; set; }
    public DateTime PickupDateTime { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string PickupPostCode { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public string DestinationPostCode { get; set; } = string.Empty;
    public string? PassengerName { get; set; }
}
