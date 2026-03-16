using TaxiDispatch.Data.Models;

namespace TaxiDispatch.DTOs.Admin;

public class TurnDownSummaryDto
{
    public List<TurnDown> Data { get; set; } = new();
    public decimal Total { get; set; }
    public int Count { get; set; }
}
