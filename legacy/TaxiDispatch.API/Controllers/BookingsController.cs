#nullable disable
using TaxiDispatch.Data.Models;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.MessageTemplates;
using TaxiDispatch.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Org.BouncyCastle.Ocsp;
using static TaxiDispatch.Services.RevoluttService;


namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    public class BookingsController : ControllerBase
    {
        private readonly TariffService _tariffService;
        private readonly AccountsService _accountsService;
        private readonly ILogger<BookingsController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly BookingService _bookingService;
        private readonly DispatchService _dispatchService;
        private readonly UserActionsService _actionLog;
        private readonly GoogleCalendarService _gcal;
        private readonly UserManager<AppUser> _userManager;
        private readonly UserProfileService _userProfileService;
        private readonly AceMessagingService _messagingService;
        private readonly RevoluttService _revoluttService;

        public BookingsController(
            IMemoryCache memoryCache,
            UserManager<AppUser> userManager,
            BookingService bookingService, 
            AccountsService accountsService,
            TariffService tariff,
            UserProfileService userProfileService,
            IHttpContextAccessor httpContextAccessor, 
            AceMessagingService messaging,
            DispatchService dispatchService,
            RevoluttService revolutt,
            UserActionsService actionLog,ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;    
            //_gcal = googleCalendar;
            _userManager = userManager;
            _tariffService = tariff;
            _accountsService = accountsService;
            _logger = logger;
            _memoryCache = memoryCache;
            _userProfileService = userProfileService;
            _messagingService = messaging;
            _revoluttService = revolutt;
            _dispatchService = dispatchService;
            _actionLog = actionLog;
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("Today")]
        public async Task<IActionResult> GetBookingsToday()
        {
            var res = await GetBookings(new GetBookingsRequestDto
            {
                From = DateTime.Now,
                To = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59)
            });
            return res;
        }

        [ApiAuthorize]
        [HttpGet("FindByTerm")]
        public async Task<IActionResult> FindByTerm(string term)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname)) 
            {
                var user = await _userManager.FindByNameAsync(uname);
                var roles = await _userProfileService.GetUserRoles(user);

                if (roles != null)
                {
                    if (roles.Contains("Driver"))
                    {
                        var res = await _bookingService.KeywordSearch(term);
                        res.Results.Clear();

                        return Ok(res);
                    }
                    else
                    {
                        var res = await _bookingService.KeywordSearch(term);

                        return Ok(res);
                    }
                }
            }

            return BadRequest();
        }


        [ApiAuthorize]
        [HttpPost("FindBookings")]
        public async Task<IActionResult> FindBookings(BookingSearchRequestDto dto)
        {
            var data = await _bookingService.FindBookings(dto);
            return Ok(data);
        }

        [ApiAuthorize]
        [HttpGet("FindById")]
        public async Task<IActionResult> GetById(int bookingId)
        {
            var data = await _bookingService.GetBooking(bookingId);
            return Ok(data);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("DateRange")]
        public async Task<IActionResult> GetBookings(GetBookingsRequestDto request)
        {
            try 
            {
                if (request.IsValid)
                {
                    GetBookingsResponseDto dto = null;

                    var uname = User.Identity.Name;

                    if (string.IsNullOrEmpty(uname))
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, $"The user identity was invalid");
                    }

                    var user = await _userManager.FindByNameAsync(uname);

                    if (user == null)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, $"The user was not returned by _userManager.\r\n username: {uname}");
                    }

                    //if(!_memoryCache.TryGetValue("bookings",out dto))
                    {
                        dto = await _bookingService.GetBookings(request.From, request.To,user.Id);

                        //_memoryCache.Set("bookings", dto, new MemoryCacheEntryOptions 
                        //{ 
                        //    SlidingExpiration = TimeSpan.FromSeconds(60)
                        //}.SetAbsoluteExpiration(TimeSpan.FromSeconds(60)));
                    }
                    
                    return Ok(dto);
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex,$"{request}");    

                return StatusCode(StatusCodes.Status500InternalServerError,ex.Message);
            }

            return BadRequest(request.Error);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("Cancel")]
        public async Task<IActionResult> CancelBooking(CancelBookingRequest request)
        {
            if(request.BookingId > 0)
            {
                var uname = User.Identity.Name;

                //SentrySdk.CaptureMessage($"User.Identity.Name: {uname}");

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        request.CancelledByName = user.FullName;
                        request.ActionByUserId = user.Id;
                    }
                }

                var res = await _bookingService.CancelBooking(request);

                if (res.Success)
                {
                    if(!request.CancelledOnArrival)
                        await _actionLog.LogBookingCancelled(request.BookingId, request.CancelledByName);
                    else
                        await _actionLog.LogBookingCOA(request.BookingId, request.CancelledByName);

                    if (request.SendEmail.HasValue)
                    {
                        if (request.SendEmail.Value)
                        {
                            // get acccount number from booking id
                            var data = await _bookingService.GetCancelBookingEmailData(request.BookingId);

                            if (data != null) 
                            {
                                if (!string.IsNullOrEmpty(data.Value.email))
                                {
                                    var dto = new BookingCancelledEmail();
                                    dto.accno = data.Value.accno;
                                    dto.pickupaddress = data.Value.pick;
                                    dto.destinationaddress = data.Value.drop;
                                    dto.passengername = data.Value.passengerName;

                                    // send booking cancelled email here
                                    await _messagingService
                                        .SendAccountBookingCancelledEmail(data.Value.email, data.Value.bookerName,dto);
                                }
                            }
                        }
                    }

                    return Ok(new ResponseDTO { Success = true }); 
                }
                else
                    return BadRequest(new ResponseDTO { Success = false, Error = res.ErrorMessage });
            }

            return BadRequest(new ResponseDTO { Success = false, Error = $"Invalid booking ID: {request.BookingId}" });
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("RemoveCOA")]
        public async Task<IActionResult> RemoveCancellOnArrival(int bookingId)
        {
            await _bookingService.RemoveCancellOnArrival(bookingId);
            return Ok();
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("Update")]
        public async Task<IActionResult> UpdateBooking(UpdateBookingRequestDto request)
        {
            if (request.IsValid)
            {
                var uname = User.Identity.Name;

                if (request.UserId == 0)
                    request.UserId = null;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        request.UpdatedByName = user.FullName;
                        request.ActionByUserId = user.Id;
                    }
                }
                var res = await _bookingService.UpdateBooking(request);

                if (res.Success)
                {
                    await _actionLog.LogBookingUpdated(request.BookingId, request.UpdatedByName);
                    return Ok(new ResponseDTO { Success = true, Value = new { Entrys = res.Value, res.Value.Count } });
                }
                else
                {
                    return BadRequest(new ResponseDTO { Success = false, Error = res.ErrorMessage });
                }
            }

            return BadRequest(request.Error);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("UpdateDate")]
        public async Task<IActionResult> UpdateBookingDate(UpdateBookingDateRequest request)
        {
            if (request.BookingId > 0)
            {
                var uname = User.Identity.Name;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        request.UpdatedByName = user.FullName;
                        request.ActionByUserId = user.Id;
                        
                    }
                }
                var res = await _bookingService.UpdateBookingDate(request);

                if (res.Success)
                {
                    await _actionLog.LogBookingUpdated(request.BookingId, request.UpdatedByName);
                    return Ok(new ResponseDTO { Success = true });
                }
                else
                {
                    return BadRequest(new ResponseDTO { Success = false, Error = res.ErrorMessage });
                }
            }

            return BadRequest(new ResponseDTO { Success = false, Error = $"Invalid booking ID: {request.BookingId}" });
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> CreateBooking(CreateBookingRequestDto request)
        {
            if (request.IsValid)
            {
                var uname = User.Identity.Name;

                if (request.UserId == 0)
                    request.UserId = null;

                if (Environment.MachineName == "i7")
                {
                    if (uname == null)
                        uname = "peter";
                }

                var fname = string.Empty;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        request.BookedByName = user.FullName;
                        request.ActionByUserId = user.Id;
                        fname = user.FullName;
                    }
                }

                var res = await _bookingService.CreateBooking(request,request.Allocate);

                List<CreatedBookingResultDto> createdList;

                var block = false;
                if (string.IsNullOrEmpty(request.RecurrenceRule))
                {
                    //if (res.UserId != 0 && res.UserId != null)
                    //{
                    //    await _dispatchService.AllocateBooking(
                    //        new AllocateBookingDto
                    //        {
                    //            BookingId = res.Id,
                    //            UserId = res.UserId,
                    //            ActionByUserId = res.ActionByUserId
                    //        });
                    //}
                }
                else
                {
                    block = true;
                }

                var id = res.First().BookingId;
                await _actionLog.LogBookingCreated(id, fname, block);
                
                return Ok(new ResponseDTO { Success = true, Value = new { Entrys = res, res.Count } });
            }

            return BadRequest(new ResponseDTO { Success = false, Error = request.Error });
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("RankCreate")]
        public async Task<IActionResult> RankPickup(RankBookingDto dto)
        {
            var req = new CreateBookingRequestDto
            {
                AccountNumber = 9999,
                DateCreated = DateTime.Now.ToUKTime(),
                Passengers = 1,
                PickupAddress = dto.Pickup,
                PickupPostCode = dto.PickupPostcode,
                PassengerName = dto.Name,
                DestinationAddress = dto.Destination,
                DestinationPostCode = dto.DestinationPostcode,
                PickupDateTime = DateTime.Now.ToUKTime(),
                ConfirmationStatus = Domain.ConfirmationStatus.Confirmed,
                Scope = Domain.BookingScope.Rank,
                UserId = dto.Userid,
                Price = (decimal)dto.Price,
                Allocate = false
            };

            return await CreateBooking(req);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("CancelByDateRange")]
        public async Task<IActionResult> CancelJobsByDateRange(DeleteByRangeRequestDto dto)
        {
            if (dto.IsValid)
            {
                var uname = User.Identity.Name;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        if (dto.AccountNo > 0 && dto.From <= dto.To)
                        { 
                            var cnt = await _bookingService.CancelBookingsByDateRange(dto, user.FullName);

                            var str = $"{dto.AccountNo} | {dto.From.ToShortDateString()} - {dto.To.Value.ToShortDateString()}";
                            await _actionLog.LogBookingCancellByDateRange(user.FullName,str);

                            return Ok(cnt);
                        }
                        else
                            return BadRequest("The data submitted was invalid, either the account number was not set or the 'from' date is greater than the 'to' date. ");
                    }
                }
            }
            
            return BadRequest(dto.Error);
        }


        [ApiAuthorize]
        [HttpGet]
        [Route("PickupHistory")]
        public async Task<IActionResult> GetPickupHistory(string phoneno)
        {
            if (string.IsNullOrEmpty(phoneno))
                return BadRequest("Invalid phone number");

            var data = await _bookingService.GetPickupAddressHistory(phoneno);

            return Ok(data);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("SoftAllocate")]
        public async Task<IActionResult> SoftAllocate(AllocateBookingDto request)
        {
            if (request.IsValid)
            {
                var uname = User.Identity.Name;

                if (request.UserId == 0)
                    request.UserId = null;

                var fname = string.Empty;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        request.ActionByUserId = user.Id;
                        fname = user.FullName;
                    }
                }

                var res = await _bookingService.SoftAllocate(request);

                if (res.Success)
                { 
                    if(request.UserId != null)
                        await _actionLog.LogBookingSoftAllocate(request.BookingId, fname);
                    else
                        await _actionLog.LogBookingSoftUnAllocate(request.BookingId, fname);

                    return Ok(new ResponseDTO { Success = true }); 
                }
                else
                    return BadRequest(new ResponseDTO { Success = false, Error = res.ErrorMessage });
            }

            return BadRequest(request.Error);
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("ConfirmAllSoftAllocates")]
        public async Task<IActionResult> ConfirmAllSoftAllocates(DateTime forDate)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    var data = await _bookingService.SoftAllocateConfirmAll(forDate);

                    foreach (var item in data.Value)
                    {
                        await Allocate(new AllocateBookingDto
                        {
                            ActionByUserId = user.Id,
                            BookingId = item.BookingId,
                            UserId = item.UserId
                        });
                    }
                }
            }
            
            return Ok();
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("RestoreCancelled")]
        public async Task<IActionResult> RestoreCancelled(int bookingId)
        {
            await _bookingService.RestoreCancelledJob(bookingId);
            return Ok();
        }


        [ApiAuthorize]
        [HttpPost]
        [Route("Allocate")]
        public async Task<IActionResult> Allocate(AllocateBookingDto request)
        {
            if (request.IsValid)
            {
                var uname = User.Identity.Name;
                
                if (request.UserId == 0)
                    request.UserId = null;

                var fname = string.Empty;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        request.ActionByUserId = user.Id;
                        fname = user.FullName;
                    }
                }

                var res = await _dispatchService.AllocateBooking(request);

                if (res.Success)
                {
                    if(request.UserId != null)
                        await _actionLog.LogBookingAllocated(request.BookingId, fname);
                    else
                        await _actionLog.LogBookingUnAllocated(request.BookingId, fname);

                    return Ok(new ResponseDTO { Success = true }); 
                }
                else
                    return BadRequest(new ResponseDTO { Success = false, Error = res.ErrorMessage });
            }

            return BadRequest(request.Error);
        }

        [HttpPost]
        [Route("GetPrice")]
        public async Task<IActionResult> GetPrice(GetPriceRequestDto obj)
        {
            GetPriceResponseDto response = null;

            if (!obj.IsValid)
            {
                return BadRequest("Invalid data or missing required fields");
            }

            if (obj.AccountNo == 9014 || obj.AccountNo == 10026 || obj.AccountNo == 10031)
            {
                try
                {
                    response = await _tariffService.GetPriceHVS(obj.PickupPostcode, obj.DestinationPostcode, obj.ViaPostcodes);
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
                }
            }
            else if (obj.AccountNo == 9999)
            {
                try
                {
                    response = await _tariffService.Get9999CashPrice(obj.PickupDateTime, obj.Passengers, obj.PickupPostcode,
                        obj.DestinationPostcode, obj.ViaPostcodes, obj.PriceFromBase);
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
                }
            }
            else
            {
                // hack to get consistent postcodes
                //if (obj.PickupPostcode == "SP7 9LP")
                //{
                //    obj.PickupPostcode = obj.DestinationPostcode;
                //    obj.DestinationPostcode = "SP7 9LP";
                //}

                response = await _tariffService.GetOnInvoicePrices(obj.PickupPostcode, obj.DestinationPostcode,
                  obj.ViaPostcodes, obj.AccountNo);
            }

            response.PriceDriver = Math.Round(response.PriceDriver, 2);
            response.PriceAccount = Math.Round(response.PriceAccount, 2);
            response.DeadMileage = Math.Round(response.DeadMileage, 2);

            return Ok(response);
        }


        [ApiAuthorize]
        [HttpPost]
        [Route("UpdateQuote")]
        public async Task<IActionResult> UpdateQuote(UpdateBookingQuoteRequestDto request)
        {
            if (request.IsValid)
            {
                await _accountsService.PriceBookingByMileage(request);
            }

            return BadRequest(new ResponseDTO { Success = false, Error = request.Error });
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("ManualPrice")]
        public async Task<IActionResult> ManualPriceUpdate(ManualPriceUpdateRequestDto request)
        {
            if (request.IsValid)
            {
                await _accountsService.ManualPriceUpdate(request);
                return Ok();
            }

            return BadRequest(new ResponseDTO { Success = false, Error = request.Error });
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("Complete")]
        public async Task<IActionResult> Complete(CompleteJobRequestDto request)
        {
            if (request.IsValid)
            {
                var uname = User.Identity.Name;

                var fname = string.Empty;
                var id = 0;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        fname = user.FullName;
                        id = user.Id;
                    }
                }

                var isAdmin = User.IsInRole("Admin");

                await _dispatchService.Complete(request,id,isAdmin);
                
                await _actionLog.LogBookingCompleted(request.BookingId, fname);

                return Ok();
            }

            return BadRequest(new ResponseDTO { Success = false, Error = request.Error });
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("SendConfirmationText")]
        public async Task<IActionResult> SendCustomerConfirmation(string phone, string date, int bookingId)
        {
            _messagingService.SendCustomerOnBookedSMS(phone, date, bookingId.ToString());

            var uname = User.Identity.Name;
            var fname = string.Empty;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    fname = user.FullName;
                }
            }

            await _actionLog.LogBookingSendConfirmationText(bookingId, fname);

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("PaymentLink")]
        public async Task<IActionResult> SendPaymentLink(int bookingId, string? telephone, string? email, string name, double price, string pickup)
        {
            if(string.IsNullOrEmpty(telephone) && string.IsNullOrEmpty(email))
                return BadRequest("No contact details provided");

            var uname = User.Identity.Name;

            var addVat = await _revoluttService.IsChargeVatOnCard();

            decimal vatAmount = 0.0M;

            // check if we have to add vat 
            if (addVat)
            {
                var vatprice = price * 1.2;
                // get the vat amount
                vatAmount = (decimal)(vatprice - price);
                // update price inclusive vat
                price = vatprice;
            }

            var nprice = price * 100;
            var plink = await _revoluttService.CreateOrder(nprice, pickup);
            var link = plink.checkout_url;
            var id = plink.id;

            if (!string.IsNullOrEmpty(link))
            {
                var url = await _messagingService.ShorternUrl(link);

                // change to card payment
                await _bookingService.SetScope(bookingId, Domain.BookingScope.Card);

                // update status 
                await _bookingService.SetPaymentStatus(bookingId, Domain.PaymentStatus.Pending);

                // set revolutt order_id
                await _bookingService.SetPaymentOrderId(bookingId, id, url, uname);

                // flag with vatAdded
                if (addVat)
                {
                    await _bookingService.FlagVatAddedOnCardAmount(bookingId,vatAmount, (decimal)price);
                }

                if (!string.IsNullOrEmpty(telephone))
                {
                    _messagingService.SendPaymentLinkSMS(telephone, url);
                }
                if (!string.IsNullOrEmpty(email))
                {
                    await _messagingService.SendPaymentLinkEmail(email, name, new PaymentLinkTemplateDto { customer = name, link = url });
                }

                var fname = string.Empty;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        fname = user.FullName;
                    }
                }

                await _actionLog.LogBookingPaymentLinkSent(bookingId, fname);

                return Ok();
            }

            return BadRequest("empty link");
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("ReminderPaymentLink")]
        public async Task<IActionResult> ResendPaymentLink(int bookingId, string phone)
        {
            var data = await _bookingService.GetPaymentLink(bookingId);

            if (!string.IsNullOrEmpty(data))
                _messagingService.SendPaymentLinkReminderSMS(phone, data);

            var uname = User.Identity.Name;
            var fname = string.Empty;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    fname = user.FullName;
                }
            }

            await _bookingService.UpdatePaymentLinkSentDate(bookingId,fname);

            await _actionLog.LogBookingPaymentLinkReminderSent(bookingId, fname);

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("RefundPayment")]
        public async Task<IActionResult> RefundPayment(int bookingId, double price)
        {
            var nprice = price * 100;

            var str = await _bookingService.GetPaymentOrderId(bookingId);

            // refund revolutt
            await _revoluttService.RefundOrder(str, nprice);

            // update status 
            await _bookingService.SetPaymentStatus(bookingId, Domain.PaymentStatus.Select);

            // set revolutt order_id
            await _bookingService.SetPaymentOrderId(bookingId, "","","");

            var uname = User.Identity.Name;
            var fname = string.Empty;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    fname = user.FullName;
                }
            }

            await _actionLog.LogBookingPaymentRefund(bookingId, fname);

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("UpdatePaymentStatus")]
        public async Task<IActionResult> UpdatePaymentStatus()
        {
            var ids = await _bookingService.GetPaymentIds();

            foreach (var id in ids) 
            {
                var status = await _revoluttService.GetOrderStatus(id);

                if (status != null) 
                {
                    if (status.state == "completed")
                    {
                        var bid = await _bookingService.UpdatePaymentStatus(id);

                        if (bid.Success)
                        {
                            await _actionLog.LogPaymentStatusUpdate(bid.Value);
                            await _actionLog.LogBookingPaymentReceiptSent(bid.Value, "Revolut");
                        }
                    }
                }
            }

            return Ok();
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("SendQuote")]
        public async Task<IActionResult> SendQuote(SendQuoteDto req)
        {
            if (req.IsValid)
            {
                if (req.ReturnTime.HasValue)
                {
                    var res = await _tariffService.Get9999CashPrice(req.ReturnTime.Value, req.Passengers, req.Pickup, req.Destination, req.Vias, true);

                    if (res != null)
                    {
                        req.ReturnPrice = res.PriceAccount;
                    }
                }

                if (!string.IsNullOrEmpty(req.Email))
                {
                    _messagingService.SendCustomerQuoteEmail(req);
                }

                if(!string.IsNullOrEmpty(req.Phone))
                    _messagingService.SendCustomerQuoteSMS(req);
            }

            var uname = User.Identity.Name;
            var fname = string.Empty;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    fname = user.FullName;
                }
            }

            await _actionLog.LogSendQuote(req.Phone,fname);

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("SendPaymentReceipt")]
        public async Task<IActionResult> SendPaymentReceipt(int bookingId)
        {
            var scope = await _bookingService.GetScope(bookingId);

            var type = scope == Domain.BookingScope.Card ? "Card - Payment Link" : "Cash - Paid to Driver";

            var request = HttpContext.Request;
            string domainUrl = $"{request.Scheme}://{request.Host}";

            await _bookingService.CreateAndSendPaymentReceipt(bookingId, type, domainUrl);

            var uname = User.Identity.Name;
            var fname = string.Empty;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    fname = user.FullName;
                }
            }

            await _actionLog.LogBookingPaymentReceiptSent(bookingId, fname);

            return Ok();
        }

        [HttpGet]
        [Route("DownloadReceipt")]
        public async Task<IActionResult> GetPaymentReceipt(int bookingId)
        {
            var fname = $"receipt-{bookingId}.pdf";
            var filePath = Path.Combine("Data\\Receipts", fname);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Message = "File not found." });
            }

            try
            {
                // Read the file into a stream
                var memory = new MemoryStream();
                await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }

                memory.Position = 0; // Reset stream position

                // Determine content type (you might want to use a library like MimeTypesMap)
                string contentType = "application/octet-stream";

                return File(memory, contentType, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                // Log the error (implement logging as per your project standards)
                return StatusCode(500, new { Message = "An error occurred while retrieving the file.", Details = ex.Message });
            }
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("CreateRevWebHook")]
        public async Task<IActionResult> CreateRevoluttWebHook()
        {
            await _revoluttService.CreateWebHook();
            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("ClearRevWebHooks")]
        public async Task<IActionResult> ClearRevoluttWebHooks()
        {
            var lst = await _revoluttService.GetWebHookList();

            if (lst != null)
            {
                foreach (var item in lst)
                {
                    await _revoluttService.DeleteWebHook(item.id);
                }
            }

            return Ok();
        }

        [HttpPost]
        [Route("RevPaymentUpdate")]
        public async Task<IActionResult> RevPaymentUpdate([FromBody] WebHookCallback req)
        {
            if (req.@event == "ORDER_AUTHORISED")
            { 
                var bid = await _bookingService.UpdatePaymentStatus(req.order_id);

                if (bid.Success)
                {
                    await _actionLog.LogPaymentStatusUpdate(bid.Value);
                    await _actionLog.LogBookingPaymentReceiptSent(bid.Value, "Revolutt");
                }
            }

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("RecordTurnDown")]
        public async Task<IActionResult> RecordTurnDown(int amount)
        {
            var date = DateTime.Now.ToUKTime();

            await _bookingService.RecordTurnDown(date, amount);

            var uname = User.Identity.Name;
            var fname = string.Empty;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    fname = user.FullName;
                }
            }

            await _actionLog.LogTurnDown(amount, fname);

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("GetActionLogs")]
        public async Task<IActionResult> GetActionLogs()
        {
            var data = await _actionLog.GetLogs();
            return Ok(data);
        }


        //[ApiAuthorize]
        [HttpGet]
        [Route("GetDuration")]
        public async Task<IActionResult> GetDuration(DateTime pickupDate, string pickupPostcode, string destinationPostcode)
        {
            // get time and miles - get price
            var journey = await _tariffService.Get9999CashPrice(pickupDate, 1, pickupPostcode,
                   destinationPostcode, null, true);

            if (journey != null)
            {
                return Ok(journey.JourneyMinutes);
            }

            return BadRequest("Possible postcode error, please check they are valid postcodes.");
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("MergeBookings")]
        public async Task<IActionResult> MergeBookings(int primaryBookingId, int appendBookingId)
        { 
            var res = await _bookingService.MergeBookings(primaryBookingId, appendBookingId);
            if (res.Error)
                return BadRequest(res.Detail);
            else
            {
                var uname = User.Identity.Name;
                var fname = string.Empty;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        fname = user.FullName;
                    }
                }

                await _actionLog.LogBookingMerged(primaryBookingId, appendBookingId,fname);
                return Ok(res.Detail); 
            }
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("CreateCOAEntry")]
        public async Task<IActionResult> CreateCOAEntry(int accno, DateTime journeyDate, string passengerName, string pickupAddress)
        {
            await _bookingService.RecordCOAEntry(accno, journeyDate, passengerName, pickupAddress);
            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("GetCOAEntrys")]
        public async Task<IActionResult> GetCOAEntrys(DateTime date)
        {
            var data = await _bookingService.GetCOAEntrys(date);
            return Ok(data);
        }

        [HttpGet("test")]
        public IActionResult TestLog()
        {
            _logger.LogInformation("TEST LOG FROM CONTROLLER");
            return Ok("Logged");
        }

        //[ApiAuthorize]
        //[HttpGet]
        //[Route("Import")]
        //public async Task<IActionResult> ImportFromGoogleCalendar()
        //{
        //    var data = await _gcal.GetGoogleEvents(DateTime.Now, null);

        //    var inserts = _gcal.ConvertToBookings(data);

        //    return Ok();
        //}
    }
}




