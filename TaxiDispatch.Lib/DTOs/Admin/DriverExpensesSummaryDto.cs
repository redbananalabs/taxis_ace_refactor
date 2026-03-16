using TaxiDispatch.Data.Models;

namespace TaxiDispatch.DTOs.Admin;

public class DriverExpensesSummaryDto
{
    public List<DriverExpense> Data { get; set; } = new();
    public decimal Total { get; set; }
}
