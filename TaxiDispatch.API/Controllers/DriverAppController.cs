#nullable disable
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.User.Requests;
using TaxiDispatch.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaxiDispatch.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverAppController : ControllerBase
    {
        private readonly UserProfileService _profileService;
        private BookingService _bookingService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DriverAppController> _logger;
        private readonly DispatchService _dispatchService;
        private readonly AccountsService _accountsService;
        private readonly AvailabilityService _availabilityService;
        private readonly DocumentService _documentService;
        private readonly UINotificationService _notificationService;
        private readonly UserActionsService _actions;
        private readonly ReportingService _reportingService;
        private readonly DriverAppService _driverAppService;

        public DriverAppController(
            UserProfileService profileService,
            BookingService bookingService,
            DispatchService dispatchService,
            UserManager<AppUser> userManager,
            AccountsService accountsService,
            AvailabilityService availabilityService,
            UINotificationService notificationService,
            ReportingService reportingService,
            DocumentService documentService,
            UserActionsService actions,
            DriverAppService driverAppService,
            ILogger<DriverAppController> logger)
        {
            _logger = logger;
            _profileService = profileService;
            _bookingService = bookingService;
            _dispatchService = dispatchService;
            _userManager = userManager;
            _accountsService = accountsService;
            _availabilityService = availabilityService;
            _documentService = documentService;
            _notificationService = notificationService;
            _actions = actions;
            _reportingService = reportingService;
            _driverAppService = driverAppService;

        }

        [ApiAuthorize]
        [HttpGet]
        [Route("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _profileService.FindByName(uname);

                if (user == null)
                {
                    return NotFound("the user was not found.");
                }

                var profile = await _driverAppService.GetProfileAsync(user.Id, user.FullName, user.PhoneNumber, user.Email);

                if (profile == null)
                {
                    return NotFound("the user was found but a profile was not created for this user.");
                }

                return Ok(profile);
            }
            else
            {
                return BadRequest("error retrieving the username from identity");
            }
        }

        [HttpGet]
        [Route("RetrieveJobOffer")]
        public async Task<IActionResult> RetrieveJobOffer(string guid)
        {
            var data = await _dispatchService.GetJobOfferEntry(guid);

            if (data != null)
            {
                return Ok(data.Data);
            }

            return NotFound();
        }

        #region SCHEDULED TASKS - CRON JOBS

        [HttpGet]
        [Route("RefreshJobOffers")]
        public async Task<IActionResult> RefreshJobOffers()
        {
            await _dispatchService.RefreshJobOffers();
            return Ok();
        }

        //[HttpGet]
        //[Route("PersistGPSLocations")]
        //public async Task<IActionResult> PersistGPSLocations()
        //{
        //    return Ok();
        //}

        #endregion

        [ApiAuthorize]
        [HttpGet]
        [Route("GetJobOffers")]
        public async Task<IActionResult> GetJobOffers()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    var data = await _dispatchService.GetJobOffers(user.Id);

                    if (data != null)
                    {
                        var res = data.Select(o => new { o.BookingId, o.BookingDateTime, o.Guid }).ToList();
                        return Ok(res);
                    }

                    //return 201
                    return StatusCode(StatusCodes.Status201Created);
                }
                else
                    return NotFound("driver not found");
            }

            return NotFound("user not found");
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("JobOfferReply")]
        public async Task<IActionResult> JobOfferReply(int jobno, AppJobOffer response, string guid)
        {

            _logger.LogInformation($"JobOfferReply called with jobno: {jobno}, response: {response}, guid: {guid}");
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    // check the offer is still valid
                    var data = await _dispatchService.GetJobOfferEntry(guid);

                    if (data != null)
                    {
                        await _dispatchService.AcceptReject(user.Id, user.FullName, jobno, response);

                        _logger.LogInformation($"JobOfferReply processed for jobno: {jobno}, response: {response}, guid: {guid}");

                        // remove job offer notification entry
                        if (!string.IsNullOrEmpty(guid))
                            await _dispatchService.DeleteJobOfferEntry(guid);

                        return Ok();
                    }
                    else
                    {
                        _logger.LogWarning($"JobOfferReply failed - job offer not found or expired for jobno: {jobno}, guid: {guid}");
                        return BadRequest("The job offer has expired or is invalid.");
                    }
                }
                else
                    return NotFound("driver not found");
            }

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("NoJob")]
        public async Task<IActionResult> JobStatusReply(int bookingId)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    await _notificationService.AddNoJobNotification(user.Id, user.FullName,bookingId);
                    return Ok();
                }
                else
                    return NotFound("driver not found");
            }

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("JobStatusReply")]
        public async Task<IActionResult> JobStatusReply(int jobno, AppJobStatus status)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    await _dispatchService.UpdateJobStatus(user.Id, jobno, status);
                    return Ok();
                }
                else
                    return NotFound("driver not found");
            }

            return Ok();
        }

        [ApiAuthorize]
        [HttpGet]
        [Route("DashTotals")]
        public async Task<IActionResult> DashTotals()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    var totals = new DriverDashDto();

                    var days = await _reportingService.DriverLoadTodaysTotals(user.Id);
                    var weeks = await _reportingService.DriverLoadWeeksTotals(user.Id);
                    var months = await _reportingService.DriverLoadMonthsTotals(user.Id);

                    totals.TotalJobCountToday = days.TotalJobCount;

                    totals.TotalJobCountWeek = weeks.JobsCount;

                    totals.EarningsTotalToday = days.EarnedTodayTotal;
                    totals.EarningsTotalWeek = weeks.Earnings;

                    totals.TotalJobCountMonth = months.JobsCount;

                    totals.EarningsTotalMonth = months.Earnings;

                    return Ok(totals);
                }
                else
                    return NotFound("driver not found");
            }

            return Ok();


        }

        [ApiAuthorize]
        [HttpGet]
        [Route("DriverShift")]
        public async Task<IActionResult> DriverShift(int userid, AppDriverShift status)
        {
            await _dispatchService.DriverShift(userid, status);
            return Ok();
        }

        [ApiAuthorize]
        [HttpPost]
        [Route("CompleteJob")]
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

                await _dispatchService.Complete(request, id,isAdmin);

                await _actions.LogBookingCompleted(request.BookingId, fname);

                return Ok();
            }

            return BadRequest(new ResponseDTO { Success = false, Error = request.Error });
        }

        [HttpPost]
        [Route("UpdateGPS")]
        [ApiAuthorize]
        public async Task<IActionResult> UpdateUserGPS(UpdateGpsPositionDto request)
        {
            var user = await _profileService.FindById(request.UserId);

            if (user != null)
            {
                await _profileService.UpdateGpsPosition(user, request.Longtitude, request.Latitude, request.Speed, request.Heading);
                return Ok();
            }
            else
                return BadRequest("User not found.");
        }

        [HttpPost]
        [Route("UpdateFCM")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ApiAuthorize]
        public async Task<IActionResult> UpdateFCM(UpdateFCMRequestDto request)
        {
            if (!string.IsNullOrEmpty(request.fcm))
            {
                var uname = User.Identity.Name;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _profileService.FindByName(uname);

                    if (user != null)
                    {
                        await _profileService.UpdateFCMToken(user, request.fcm);
                        return Ok(new ResponseDTO { Success = true });
                    }
                }
                return BadRequest("user not found from token");
            }
            else
            {
                return BadRequest("FCM Token is required.");
            }
        }

        [HttpGet]
        [Route("TodaysJobs")]
        [ApiAuthorize]
        public async Task<IActionResult> TodayJobs()
        {
            var uname = User.Identity.Name;
            var uid = 0;
            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _profileService.FindByName(uname);
                uid = user.Id;
            }

            var dto = await _bookingService.GetBookingsByDriver(DateTime.Now.ToUKTime(), DateTime.Now.To2359(), uid);
            dto.Bookings.RemoveAll(o => o.Status == BookingStatus.Complete);
            dto.Bookings.RemoveAll(o => o.Status == BookingStatus.RejectedJob);
            dto.Bookings.RemoveAll(o => o.Status == BookingStatus.RejectedJobTimeout);

            dto.Bookings.ForEach(o =>
            {
                if (o.Mileage == null)
                {
                    o.Mileage = 0;
                }
            });


            return Ok(dto);
        }

        [HttpGet]
        [Route("FutureJobs")]
        [ApiAuthorize]
        public async Task<IActionResult> FutureJobs()
        {
            var uname = User.Identity.Name;
            var uid = 0;
            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _profileService.FindByName(uname);
                uid = user.Id;
            }

            var date = DateTime.Now.AddDays(1).ToUKTime();
            var res = await _bookingService.GetBookingsByDriver(date, date.AddMonths(1), uid);

            res.Bookings.ForEach(o =>
            {
                if (o.Mileage == null)
                {
                    o.Mileage = 0;
                }
            });

            return Ok(res);
        }

        [HttpGet]
        [Route("CompletedJobs")]
        [ApiAuthorize]
        public async Task<IActionResult> CompletedJobs()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var from = DateTime.Now.AddDays(-7).Date;

                var to = DateTime.Now.ToUKTime();
                var user = await _profileService.FindByName(uname);

                var data = await _driverAppService.GetCompletedJobsAsync(user.Id, from, to);
                return Ok(data);
            }

            return NotFound("user not found.");
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
                        dto = await _bookingService.GetBookingsByDriver(request.From, request.To, user.Id);

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
                _logger.LogError(ex, $"{request}");

                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

            return BadRequest(request.Error);
        }

        [HttpGet]
        [Route("Earnings")]
        [ApiAuthorize]
        public async Task<IActionResult> Earnings(DateTime from, DateTime to)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    var data = await _accountsService.GetDailyEarningsWithinRange(from, to,user.Id);
                    return Ok(data);
                }

                return NotFound("user not found.");
            }

            return NotFound("the username was not found.");
        }

        [HttpGet]
        [Route("Statements")]
        [ApiAuthorize]
        public async Task<IActionResult> Statements()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    var start = DateTime.Now.ToUKTime().AddMonths(-12);
                    var end = DateTime.Now.ToUKTime();
                 
                    var data = await _accountsService.GetStatements(start, end, user.Id);

                    return Ok(data);
                }
                return NotFound("user not found.");
            }

            return NotFound("the username was not found.");
        }

        [HttpGet]
        [Route("Availabilities")]
        [ApiAuthorize]
        public async Task<IActionResult> GetAvailabilities()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);

                if (user != null)
                {
                    var start = DateTime.Now.ToUKTime().AddMonths(-12);
                    var end = DateTime.Now.ToUKTime();

                    var data = await _availabilityService.GetAvailabilities(user.Id, null);

                    return Ok(data);
                }
                return NotFound("user not found.");
            }

            return NotFound("the username was not found.");
        }

        [HttpPost]
        [Route("SetAvailability")]
        [ApiAuthorize]
        public async Task<IActionResult> SetAvailability(CreateAvailabilityRequestDto req)
        {
            if (req.IsValid)
            {
                var uname = User.Identity.Name;

                if (!string.IsNullOrEmpty(uname))
                {
                    var user = await _userManager.FindByNameAsync(uname);

                    if (user != null)
                    {
                        var start = DateTime.Now.ToUKTime().AddMonths(-12);
                        var end = DateTime.Now.ToUKTime();


                        var fhr = Convert.ToInt32(req.From.Split(':')[0]);
                        var fmm = Convert.ToInt32(req.From.Split(':')[1]);

                        var thr = Convert.ToInt32(req.To.Split(':')[0]);
                        var tmm = Convert.ToInt32(req.To.Split(':')[1]);

                        var fromTs = new TimeSpan(fhr, fmm, 0);
                        var toTs = new TimeSpan(thr, tmm, 0);

                        
                        var data = await _availabilityService.Create(req.UserId, req.Date, fromTs, toTs,
                            (bool)req.GiveOrTake, req.Type, req.Note, user.FullName);

                        if (data.Success)
                            return Ok(data);
                        else
                            return BadRequest(data.ErrorMessage);
                    }
                    return NotFound("user not found.");
                }

                return NotFound("the username was not found.");
            }

            return BadRequest("the model data was invalid or missing required fields.");
        }

        [HttpGet]
        [Route("DeleteAvailability")]
        [ApiAuthorize]
        public async Task<IActionResult> DeleteAvailability(int id)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);
            }

            await _availabilityService.Delete(id, uname);

            return Ok();
        }

        [HttpGet]
        [Route("Arrived")]
        [ApiAuthorize]
        public async Task<IActionResult> Arrived(int bookingId)
        {

            _logger.LogInformation($"DriverAppController.Arrived called for bookingId: {bookingId}");

            if (await _driverAppService.MarkArrivedAsync(bookingId))
            {
                return Ok();
            }

            return NotFound();
        }

        [HttpPost]
        [Route("AddExpense")]
        [ApiAuthorize]
        public async Task<IActionResult> AddExpense(DriverExpenseDto req)
        {
            if (req.IsValid)
            {
                await _profileService.AddDriverExpense(req);
                return Ok();
            }
            
            return BadRequest("the model data is invalid or misign required fields.");
        }

        [HttpGet]
        [Route("GetExpenses")]
        [ApiAuthorize]
        public async Task<IActionResult> GetExpenses([FromQuery] GetDriverExpensesRequestDto req)
        {
            if (req.IsValid)
            {
                var data = await _profileService.GetDriverExpenses(req.From, req.To, false, req.UserId);
                return Ok(data);
            }

            return BadRequest("the model data is invalid or misign required fields.");
        }

        [HttpPost]
        [Route("UploadDocument")]
        [ApiAuthorize]
        public async Task<IActionResult> UploadDocument(IFormFile file, DocumentType type)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file");

            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _userManager.FindByNameAsync(uname);
                
                var res = await _documentService.UploadDocument(user.Id, user.FullName,file, type);

                return Ok(res);
            }

            return NotFound("the user (sender) was not found.");
        }

        [HttpPost]
        [Route("SetActiveJob")]
        [ApiAuthorize]
        public async Task<IActionResult> SetActiveJob(int bookingId)
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _profileService.FindByName(uname);

                if (user == null)
                {
                    return NotFound("the user was not found.");
                }

                if (await _driverAppService.SetActiveJobAsync(user.Id, bookingId))
                {
                    return Ok();
                }

                return BadRequest("You are not registered as on shift, please start your shift and retry.");
            }
            else
            {
                return BadRequest("error retrieving the username from identity");
            }
        }

        [HttpGet]
        [Route("GetActiveJob")]
        [ApiAuthorize]
        public async Task<IActionResult> GetActiveJob()
        {
            var uname = User.Identity.Name;

            if (!string.IsNullOrEmpty(uname))
            {
                var user = await _profileService.FindByName(uname);

                if (user == null)
                {
                    return NotFound("the user was not found.");
                }

                var result = await _driverAppService.GetActiveJobAsync(user.Id);

                if (result.Found)
                {
                    return Ok(result.ActiveBookingId);
                }
                else
                {
                    return NotFound("No active job was found.");
                }
            }
            else
            {
                return BadRequest("error retrieving the username from identity");
            }
            
        }

        [HttpGet]
        [Route("GetStatementHeaders")]
        [ApiAuthorize]
        public async Task<IActionResult> GetStatementHeaders(DateTime from, DateTime to, int userId)
        { 
            return Ok(await _driverAppService.GetStatementHeadersAsync(from, to, userId));
        }

    }
}





