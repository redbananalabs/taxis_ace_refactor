namespace TaxiDispatch.DTOs.Driver.Responses;

public class DriverStatementHeaderDto
{
    public DateTime StatementDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalJobCount { get; set; }
    public int StatementId { get; set; }
    public double SubTotal { get; set; }
}
