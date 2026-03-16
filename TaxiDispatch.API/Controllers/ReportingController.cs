using TaxiDispatch.Domain;
using TaxiDispatch.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiAuthorize]
    public class ReportingController : ControllerBase
    {
        private readonly ReportingService _reportingService;
        public ReportingController(ReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        
        [HttpPost]
        [Route("DuplicateBookingsReport")]
        public async Task<IActionResult> DuplicateBookingsReport(DateTime startDate)
        {
            var res = await _reportingService.DuplicateBookingsReport(startDate);
            return Ok(res);
        }

        [HttpPost]
        [Route("GetBookingScopeBreakdown")]
        public async Task<IActionResult> GetBookingScopeBreakdown(DateTime from, DateTime to, ViewPeriodBy period, bool compare)
        {
            var res = await _reportingService.GetBookingScopeBreakdownAsync(from, to, period, compare);
            return Ok(res);
        }

        [HttpPost]
        [Route("GetTopCustomers")]
        public async Task<IActionResult> GetTopCustomers(DateTime from, DateTime to, BookingScope? scope, int depth = 10)
        {
            var res = await _reportingService.GetTopCustomers(from, to, scope, depth);
            return Ok(res);
        }

        [HttpPost]
        [Route("GetPickupPostcodes")]
        public async Task<IActionResult> GetPickupPostcodes(DateTime from, DateTime to, BookingScope? scope)
        {
            var res = await _reportingService.GetPickupPostcodes(from, to, scope);
            return Ok(res);
        }

        [HttpPost]
        [Route("GetVehicleTypeCounts")]
        public async Task<IActionResult> GetVehicleTypeCounts(DateTime from, DateTime to, BookingScope? scope)
        {
            var res = await _reportingService.GetVehicleTypeCounts(from, to, scope);
            return Ok(res);
        }

        [HttpPost]
        [Route("GetAverageDuration")]
        public async Task<IActionResult> GetAverageDuration(DateTime from, DateTime to, ViewPeriodBy period, BookingScope? scope)
        {
            var res = await _reportingService.GetAverageDuration(from, to, period, scope);
            return Ok(res);
        }

        [HttpPost]
        [Route("GetGrowthByPeriod")]
        public async Task<IActionResult> GetGrowthByPeriod(int startMonth, int startYear, int endMonth, int endYear,
          GroupByType viewBy)
        {
            var res = await _reportingService.GetBookingGrowthByMonthOrYear(startMonth, startYear, endMonth, endYear, viewBy);
            return Ok(res);
        }
        
        [HttpPost]
        [Route("RevenueByMonth")]
        public async Task<IActionResult> RevenueByMonth(DateTime from, DateTime to)
        {
            var res = await _reportingService.RevenueByMonth(from,to);
            return Ok(res);
        }

        [HttpPost]
        [Route("PayoutsByMonth")]
        public async Task<IActionResult> PayoutsByMonth(DateTime from, DateTime to)
        {
            var res = await _reportingService.PayoutsByMonth(from, to);
            return Ok(res);
        }

        [HttpPost]
        [Route("ProfitabilityOnInvoices")]
        public async Task<IActionResult> ProfitabilityOnInvoices(DateTime from, DateTime to)
        {
            var res = await _reportingService.ProfitabilityOnInvoices(from, to);
            return Ok(res);
        }

        [HttpPost]
        [Route("TotalProfitabilityByPeriod")]
        public async Task<IActionResult> TotalProfitabilityByPeriod(DateTime from, DateTime to)
        {
            var res = await _reportingService.TotalProfitabilityByPeriod(from, to);
            return Ok(res);
        }

        [HttpPost]
        [Route("ProfitsByDateRange")]
        public async Task<IActionResult> ProfitsByDateRange(DateTime from, DateTime to)
        {
            var res = await _reportingService.GetProfitsByDateRange(from, to);
            return Ok(res);
        }

        [HttpGet]
        [Route("GetQRScans")]
        public async Task<IActionResult> GetQRCounts()
        {
            var res = await _reportingService.GetQRCodeScans();
            return Ok(res);
        }

    }
}


