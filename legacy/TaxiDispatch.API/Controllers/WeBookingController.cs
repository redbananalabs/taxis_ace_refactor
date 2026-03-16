#nullable disable
using Microsoft.AspNetCore.Mvc;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Admin;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.Services;

namespace TaxiDispatch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WeBookingController : ControllerBase
{
    private readonly WebBookingService _webBookingService;

    public WeBookingController(WebBookingService webBookingService)
    {
        _webBookingService = webBookingService;
    }

    [ApiAuthorize]
    [HttpPost]
    [Route("CreatePolygon")]
    public async Task<IActionResult> CreatePolygon([FromBody] CreateGeoFenceDto dto)
    {
        var result = await _webBookingService.CreatePolygonAsync(dto);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpPost]
    [Route("UpdatePolygon")]
    public async Task<IActionResult> UpdatePolygon([FromBody] CreateGeoFenceDto dto)
    {
        var result = await _webBookingService.UpdatePolygonAsync(dto);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpDelete]
    [Route("DeletePolygon")]
    public async Task<IActionResult> DeletePolygon(string polygonName)
    {
        var result = await _webBookingService.DeletePolygonAsync(polygonName);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpGet]
    [Route("GetAllPolygons")]
    public async Task<IActionResult> GetAllPolygons()
    {
        var result = await _webBookingService.GetAllPolygonsAsync();
        return ToActionResult(result);
    }

    [HttpPost]
    [Route("GetAdressSuggestions")]
    public async Task<IActionResult> GetAdressSuggestions(string search)
    {
        var result = await _webBookingService.GetAddressSuggestionsAsync(search);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpPost]
    [Route("AddNewPassenger")]
    public async Task<IActionResult> AddNewPassenger([FromBody] AccountPassengerDto dto)
    {
        var result = await _webBookingService.AddNewPassengerAsync(dto);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpDelete]
    [Route("DeletePassenger")]
    public async Task<IActionResult> DeletePassenger(int passengerId)
    {
        var result = await _webBookingService.DeletePassengerAsync(passengerId);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpGet]
    [Route("GetPassengers")]
    public async Task<IActionResult> GetPassengerList(int accountNo)
    {
        var result = await _webBookingService.GetPassengersAsync(accountNo);
        return ToActionResult(result);
    }

    [HttpPost]
    [Route("CreateWebBooking")]
    public async Task<IActionResult> CreateWebBooking(WebBookingDto dto)
    {
        var result = await _webBookingService.CreateWebBookingAsync(dto);
        return ToActionResult(result);
    }

    [HttpPost]
    [Route("CreateCashBooking")]
    public async Task<IActionResult> CreateCashBooking(CashWebBookingDto dto)
    {
        var result = await _webBookingService.CreateCashBookingAsync(dto);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpPost]
    [Route("GetWebBookings")]
    public async Task<IActionResult> GetWebBookings(GetWebBookingsRequestDto dto)
    {
        var result = await _webBookingService.GetWebBookingsAsync(dto);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpPost]
    [Route("Accept")]
    public async Task<IActionResult> AcceptWebBooking(WebBookingAcceptDto dto)
    {
        var username = User?.Identity?.Name ?? string.Empty;
        var authHeader = Request.Headers["Authorization"].ToString();
        var result = await _webBookingService.AcceptWebBookingAsync(
            dto,
            username,
            authHeader,
            HttpContext.RequestAborted);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpPost]
    [Route("Reject")]
    public async Task<IActionResult> RejectWebBooking(WebBookingRejectDto dto)
    {
        var username = User?.Identity?.Name ?? string.Empty;
        var result = await _webBookingService.RejectWebBookingAsync(dto, username, HttpContext.RequestAborted);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpPost]
    [Route("AmendAccept")]
    public async Task<IActionResult> AmendAcceptWebBooking(WebBookingAmendAcceptDto dto)
    {
        var username = User?.Identity?.Name ?? string.Empty;
        var result = await _webBookingService.AmendAcceptWebBookingAsync(dto, username, HttpContext.RequestAborted);
        return ToActionResult(result);
    }

    [HttpGet]
    [Route("ShortenUrl")]
    public async Task<IActionResult> ShortenUrl(string longUrl)
    {
        var result = await _webBookingService.ShortenUrlAsync(longUrl);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpGet]
    [Route("GetDuration")]
    public async Task<IActionResult> GetDuration(int wid)
    {
        var result = await _webBookingService.GetDurationAsync(wid);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpGet]
    [Route("GetAccountActiveBookings")]
    public async Task<IActionResult> GetAccountActiveBookings(int accno)
    {
        var result = await _webBookingService.GetAccountActiveBookingsAsync(accno);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpGet]
    [Route("RequestAmendment")]
    public async Task<IActionResult> RequestAmendment(int bookingId, string message, bool block)
    {
        var username = User?.Identity?.Name ?? string.Empty;
        var result = await _webBookingService.RequestAmendmentAsync(bookingId, message, block, username);
        return ToActionResult(result);
    }

    [ApiAuthorize]
    [HttpGet]
    [Route("RequestCancellation")]
    public async Task<IActionResult> RequestCancellation(int bookingId, bool block)
    {
        var username = User?.Identity?.Name ?? string.Empty;
        var result = await _webBookingService.RequestCancellationAsync(bookingId, block, username);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult(WebBookingServiceResult result)
    {
        return result.StatusCode switch
        {
            200 => result.Value is null ? Ok() : Ok(result.Value),
            400 => BadRequest(result.Value),
            404 => NotFound(result.Value),
            _ => result.Value is null
                ? StatusCode(result.StatusCode)
                : StatusCode(result.StatusCode, result.Value)
        };
    }
}
