#nullable disable
using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.DTOs;
using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.MessageTemplates;
using TaxiDispatch.PDF;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Text;


namespace TaxiDispatch.Services
{
    public class AccountsService : BaseService<AccountsService>
    {
        private readonly AceMessagingService _messagingService;
        private readonly TariffService _tariffService;
        private readonly DocumentService _documentService;
        private readonly IMapper _mapper;

        public AccountsService(
            IDbContextFactory<TaxiDispatchContext> factory,
            AceMessagingService messagingService,
            TariffService tariffService, IMapper mapper, DocumentService documentService, 
            ILogger<AccountsService> logger) 
            : base(factory,logger)
        {
            _mapper = mapper;
            _messagingService = messagingService;
            _tariffService = tariffService;
            _documentService = documentService;
        }

        // Used for internal grouping
        public string GetBidirectionalKey(string a, string b)
        {
            var pair = new[] { a?.Trim().ToUpperInvariant(), b?.Trim().ToUpperInvariant() };
            Array.Sort(pair);
            return $"{pair[0]} <-> {pair[1]}"; // Avoid "|" in real data by using "||"
        }

        // Used for display
        public string GetReadableBidirectionalKey(string a, string b)
        {
            var pair = new[] { a?.Trim(), b?.Trim() };
            Array.Sort(pair, StringComparer.OrdinalIgnoreCase);
            return $"{pair[0]} | {pair[1]}";
        }

        public List<ChargeableGroup> GroupByBirectionalJourney(IEnumerable<ChargeableJob> jobs)
        {
            var groupedByRoute = jobs
                .GroupBy(j => GetBidirectionalKey(j.Pickup, j.Destination))
                .Select(routeGroup => new ChargeableGroup
                {
                    GroupName = routeGroup.Key,
                    Jobs = routeGroup.OrderBy(o => o.Vias?.Count).ToList()
                })
                .ToList();

            return groupedByRoute;
        }

        public List<ChargeableJobGroup> GroupBidirectionalByPassenger(IEnumerable<ChargeableJob> jobs)
        {
            return jobs
                .GroupBy(j => j.Passenger)
                .Select(passengerGroup => new ChargeableJobGroup
                {
                    Passenger = passengerGroup.Key,
                    PickupGroups = passengerGroup
                        .GroupBy(j => GetBidirectionalKey(j.PickupPostcode, j.DestinationPostcode))
                        .Select(bidirectionalGroup =>
                        {
                            var allJobs = bidirectionalGroup.ToList();

                            // Normalized pickup group name for display
                            var pickupGroupKey = GetReadableBidirectionalKey(allJobs.First().Pickup, allJobs.First().Destination);

                            // Group by bidirectional key again for destination
                            var destinationGroups = allJobs
                                .GroupBy(j => GetBidirectionalKey(j.PickupPostcode, j.DestinationPostcode))
                                .Select(destGroup =>
                                {
                                    var destJobs = destGroup.OrderBy(j => j.Date).ToList();

                                    // Determine a display-friendly destination (e.g., the one used most as destination)
                                    var canonicalDestination = destJobs
                                        .GroupBy(j => j.Destination)
                                        .OrderByDescending(g => g.Count())
                                        .Select(g => g.Key)
                                        .FirstOrDefault();

                                    canonicalDestination = destJobs.First().Pickup + " <-> " +  destJobs.First().Destination;

                                    return new DestinationGroup
                                    {
                                        Destination = canonicalDestination ?? string.Empty,
                                        Jobs = destJobs
                                    };
                                })
                                .ToList();

                            return new PickupGroup
                            {
                                Pickup = pickupGroupKey,
                                DestinationGroups = destinationGroups
                            };
                        })
                        .ToList()
                })
                .ToList();
        }


        public async Task<List<ChargeableJob>> GetChargableJobsForAccount(int accno, DateTime start, DateTime lastDate)
        {
            var list = new List<ChargeableJob>();
            List<Booking> jobs;
            var last = lastDate.To2359();

            if (accno == 0)
            {
                jobs = await _dB.Bookings.Include(o=>o.Vias).AsNoTracking()
                //jobs = await _dB.Bookings.AsNoTracking()
               .Where(o => (o.PickupDateTime.Date >= start.Date && o.PickupDateTime <= last) && o.UserId != null &&
                   o.InvoiceNumber == null && o.AccountNumber != null && o.Scope == BookingScope.Account &&
                   o.Cancelled == false)              
               .ToListAsync();
            }
            else
            {
                jobs = await _dB.Bookings.Include(o => o.Vias).AsNoTracking()
                //jobs = await _dB.Bookings.AsNoTracking()
                  .Where(o => ((o.PickupDateTime.Date >= start.Date) && (o.PickupDateTime <= last) && o.UserId != null)
                    && o.InvoiceNumber == null && o.AccountNumber == accno && o.Cancelled == false && o.Scope == BookingScope.Account)
                  .ToListAsync();
            }

            foreach (var job in jobs) 
            {
                var obj = new ChargeableJob();

                obj.BookingId = job.Id;
                obj.Date = job.PickupDateTime;
                obj.Pickup = $"{job.PickupAddress}, {job.PickupPostCode}";
                obj.PickupPostcode = job.PickupPostCode;
                obj.Destination = $"{job.DestinationAddress}, {job.DestinationPostCode}";
                obj.DestinationPostcode = job.DestinationPostCode;
                obj.Passenger = job.PassengerName;
                obj.Passengers = job.Passengers;
                obj.Vias = job.Vias;
                obj.PriceAccount = (double)job.PriceAccount;
                obj.Price = (double)job.Price;
                obj.Scope = job.Scope.Value;

                try 
                {
                    obj.UserId = job.UserId.Value;
                }
                catch
                {

                }
                
                obj.Cancelled = job.Cancelled;
                obj.COA = job.CancelledOnArrival;
                obj.VehicleType = job.VehicleType;
                obj.AccNo = job.AccountNumber;
                obj.ParkingCharge = (double)job.ParkingCharge;
                obj.WaitingMinutes = job.WaitingTimeMinutes;
                //obj.WaitingPriceDriver = (double)job.WaitingTimePriceDriver;
                obj.PostedForInvoicing = job.PostedForInvoicing;
                obj.Miles = job.Mileage.HasValue ? (double)job.Mileage.Value : 0;
                obj.PaymentStatus = job.PaymentStatus;

                list.Add(obj);
            }

            return list.OrderBy(o=>o.Date).ToList();
        }

        public async Task<List<ChargeableJob>> GetChargableJobsForDriver(BookingScope scope,DateTime lastDate, int driver = 0)
        {
            var list = new List<ChargeableJob>();

            var last = lastDate.Date.To2359();

            if (driver == 0)
            {
                list = await _dB.Bookings
                    .Where(o => o.PickupDateTime <= last &&
                        o.StatementId == null && o.UserId != null &&
                        o.Cancelled == false)
                    .Select(o => new ChargeableJob
                    {
                        BookingId = o.Id,
                        Date = o.PickupDateTime,
                        Pickup = $"{o.PickupAddress}, {o.PickupPostCode}",
                        PickupPostcode = o.PickupPostCode,
                        Destination = $"{o.DestinationAddress}, {o.DestinationPostCode}",
                        DestinationPostcode = o.DestinationPostCode,
                        Passenger = o.PassengerName,
                        Passengers = o.Passengers,
                        Vias = o.Vias.Any() ? o.Vias : new List<BookingVia>(),
                        Price = (double)o.Price,
                        Scope = (BookingScope)o.Scope,
                        UserId = o.UserId.Value,
                        Cancelled = o.Cancelled,
                        COA = o.CancelledOnArrival, 
                        VehicleType = o.VehicleType, 
                        AccNo = o.AccountNumber,
                        ParkingCharge = (double)o.ParkingCharge,
                        WaitingMinutes = o.WaitingTimeMinutes,
                        //WaitingPriceAccount = (double)o.WaitingTimePriceAccount,
                        //WaitingPriceDriver = (double)o.WaitingTimePriceDriver,
                        PostedForStatement = o.PostedForStatement,
                        PaymentStatus = o.PaymentStatus
                    })
                    .ToListAsync();
            }
            else
            {
                list = await _dB.Bookings
                    .Where(o => o.PickupDateTime <= last &&
                        o.UserId == driver && o.StatementId == null && o.Cancelled == false)
                    .Select(o => new ChargeableJob
                    {
                        BookingId = o.Id,
                        Date = o.PickupDateTime,
                        Pickup = $"{o.PickupAddress}, {o.PickupPostCode}",
                        PickupPostcode = o.PickupPostCode,
                        Destination = $"{o.DestinationAddress}, {o.DestinationPostCode}",
                        DestinationPostcode = o.DestinationPostCode,
                        Passenger = o.PassengerName,
                        Passengers = o.Passengers,
                        Vias = o.Vias.Any() ? o.Vias : new List<BookingVia>(),
                        Price = (double)o.Price,
                        Scope = (BookingScope)o.Scope,
                        UserId = o.UserId.Value,
                        Cancelled = o.Cancelled,
                        COA = o.CancelledOnArrival,
                        VehicleType = o.VehicleType,
                        AccNo = o.AccountNumber,
                        ParkingCharge = (double)o.ParkingCharge,
                        WaitingMinutes = o.WaitingTimeMinutes,
                        //WaitingPriceAccount = (double)o.WaitingTimePriceAccount,
                       // WaitingPriceDriver = (double)o.WaitingTimePriceDriver,
                        PostedForStatement = o.PostedForStatement,
                        PaymentStatus = o.PaymentStatus
                    })
                    .ToListAsync();
            }

            if (scope != BookingScope.All)
            {
                list = list.Where(o => o.Scope == scope).ToList();
            }

            return list.OrderBy(o => o.Date).ToList();
        }

        /// <summary>
        /// TODO - 
        /// ParkingCharge = (double)o.ParkingCharge,
        /// WaitingMinutes = o.WaitingTimeMinutes,
        /// WaitingPriceAccount = (double) o.WaitingTimePriceAccount,
        /// WaitingPriceDriver = (double)o.WaitingTimePriceDriver
        /// </summary>
        /// <param name="jobs"></param>
        /// <returns></returns>
        public async Task ProcessDrivers(List<ChargeableJob> jobs)
        {
            //The query uses the 'First'/'FirstOrDefault' operator without 'OrderBy' and filter operators. This may lead to unpredictable results.
            var cardRate = _dB.CompanyConfig
                .OrderBy(o => o.Id) // Or another relevant column
                .Select(o => o.CardTopupRate)
                .FirstOrDefault();

            var statements = new List<DriverInvoiceStatement>();

            var grp = jobs.GroupBy(o => o.UserId).ToList();

            var vataddedOnCard = await _dB.CompanyConfig.Select(o => o.AddVatOnCardPayments).FirstOrDefaultAsync();

            foreach (var driver in grp) 
            {
                var obj = new DriverInvoiceStatement();
                obj.StartDate = driver.Min(o => o.Date);
                obj.EndDate = driver.Max(o => o.Date);
                obj.UserId = driver.Key;

                obj.EarningsCash = driver.Where(o => o.Scope == BookingScope.Cash).Sum(o => o.Price);

                if (vataddedOnCard)
                {
                    // needs to added with cash below
                    obj.EarningsCard = driver.Where(o => o.Scope == BookingScope.Card && o.PaymentStatus == PaymentStatus.Paid).Sum(o => (o.Price/1.2));
                }
                else
                {
                    // needs to added with cash below
                    obj.EarningsCard = driver.Where(o => o.Scope == BookingScope.Card && o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.Price);
                }
                    

                obj.CashJobsTotalCount = driver.Where(o => o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card).Count();
                obj.AccountJobsTotalCount = driver.Where(o => o.Scope == BookingScope.Account).Count();

                var parking = driver.Where(o => o.Scope == BookingScope.Account).Sum(o => o.ParkingCharge);
                var waiting = driver.Where(o => o.Scope == BookingScope.Account).Sum(o => o.WaitingPriceDriver);

                obj.EarningsAccount = driver.Where(o => o.Scope == BookingScope.Account).Sum(o => o.Price);
                
                // add parking & waiting to account total
                obj.EarningsAccount += (parking + waiting);

                // get rank totals
                obj.RankJobsTotalCount = driver.Where(o => o.Scope == BookingScope.Rank).Count();
                obj.EarningsRank = driver.Where(o => o.Scope == BookingScope.Rank).Sum(o => o.Price);

                // get drivers cash commission rate
                var commsRate = await _dB.UserProfiles.Where(o => o.UserId == obj.UserId).Select(o => o.CashCommissionRate).FirstOrDefaultAsync();

                // get card fees amount
                var cardFeesTotal = (obj.EarningsCard / 100) * cardRate;

                obj.CardFees = cardFeesTotal;

                // calculate the comms for cash and card
                obj.CommissionDue = (((obj.EarningsCash + obj.EarningsCard) / 100) * commsRate + (obj.EarningsRank / 100) * 7.5) + cardFeesTotal;

                obj.SubTotal = ((obj.EarningsCash + obj.EarningsCard + obj.EarningsRank) - obj.CommissionDue) + obj.EarningsAccount; //100

                _dB.DriverInvoiceStatements.Add(obj);

                await _dB.SaveChangesAsync();

                statements.Add(obj);

                // update each booking with the statement id
                foreach (var job in driver)
                {
                    await _dB.Bookings.Where(o => o.Id == job.BookingId)
                        .ExecuteUpdateAsync(b => b.SetProperty(u => u.StatementId, obj.StatementId));
                }

                // get driver name and email
                var data = await _dB.UserProfiles
                    .Where(o => o.UserId == driver.Key)
                    .Include(o => o.User)
                    .AsNoTracking()
                    .Select(o => new { o.RegNo, o.User.FullName, o.User.Email })
                    .FirstOrDefaultAsync();

                // now send emails to user
                var statement = new DriverStatementDto
                {
                    userid = driver.Key,
                    fullname = data.FullName,
                    reg = data.RegNo,
                    statementid = obj.StatementId,
                    commstotal = obj.CommissionDue.ToString("N2"),
                    nettotal = obj.PaymentDue.ToString("N2"),
                    period = $"{obj.StartDate.ToString("dd/MM/yy")} - {obj.EndDate.ToString("dd/MM/yy")}",
                    transactions = new List<DriverStatementDto.transaction>()
                };

                foreach (var job in driver) 
                {
                    var comms = 0.0;
                    var net = 0.0;

                    if (job.Scope == BookingScope.Cash)
                    {
                        comms = (job.Price / 100) * commsRate;
                        net = (job.Price - comms);
                    }
                    else if (job.Scope == BookingScope.Card)
                    {
                        comms = (job.Price / 100) * (commsRate + cardRate);
                        net = (job.Price - comms);
                    }
                    else if (job.Scope == BookingScope.Rank)
                    {
                        comms = (job.Price / 100) * 7.5;
                        net = (job.Price - comms);
                    }
                    else
                    {
                        net = job.Price + job.WaitingPriceDriver + job.ParkingCharge;
                    }

                    statement.transactions.Add(new DriverStatementDto.transaction 
                    {
                        bookingid = job.BookingId,
                        date = job.Date.ToUKTime().ToString("dd/MM/yy HH:mm"),
                        comms = comms.ToString("N2"),
                        net = net.ToString("N2")
                    });
                }
                
                // get created statement
                var pstatement = await GetStatementById(statement.statementid);

                // generate pdf
                var res = await GenerateStatementPDF(new List<DriverInvoiceStatementDto> { pstatement });

                //await _messagingService.SendDriverStatementEmail(data.Email, data.FullName, statement);
                await _messagingService.SendDriverStatementEmail(data.Email, data.FullName, statement, res.First().Filename, res.First().Base64);
            }
        }
        
        /// <summary>
        /// TODO - 
        /// ParkingCharge = (double)o.ParkingCharge,
        /// WaitingMinutes = o.WaitingTimeMinutes,
        /// WaitingPriceAccount = (double) o.WaitingTimePriceAccount,
        /// WaitingPriceDriver = (double)o.WaitingTimePriceDriver
        /// </summary>
        /// <param name="jobs"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> ProcessAccounts(List<ChargeableJob> jobs, bool emailInvoices)
        {
            var pdfs = new Dictionary<string, string>();
            var invoices = new List<AccountInvoice>();

            var grp = jobs.GroupBy(o => o.AccNo).ToList();

            foreach (var acc in grp)
            {
                var obj = new AccountInvoice();
               
                obj.Date = DateTime.Now.ToUKTime();

                var sum = (decimal)acc.Sum(o => o.PriceAccount) +
                    (decimal)acc.Sum(o => o.WaitingPriceAccount) +
                    (decimal)acc.Sum(o => o.ParkingCharge);

                obj.NetTotal = Math.Round(sum,2);
                obj.VatTotal = Math.Round((obj.NetTotal / 100 * 20),2);
                obj.NumberOfJourneys = acc.Count();
                obj.Total = Math.Round(obj.NetTotal + obj.VatTotal,2);
                obj.AccountNo = acc.Key.Value;

                var accData = await _dB.Accounts.Where(o => o.AccNo == obj.AccountNo).AsNoTracking().FirstOrDefaultAsync();

                obj.Reference = accData.Reference;
                obj.PurchaseOrderNo = accData.PurchaseOrderNo;

                _dB.AccountInvoices.Add(obj);

                await _dB.SaveChangesAsync();

                invoices.Add(obj);

                // list to hold basic data for pdf invoice
                var journeys = new List<JourneyItem>();

                // update each booking with the invoice no
                foreach (var job in acc)
                {
                    await _dB.Bookings.Where(o => o.Id == job.BookingId)
                        .ExecuteUpdateAsync(b => b.SetProperty(u => u.InvoiceNumber, obj.InvoiceNumber));

                    journeys.Add(new JourneyItem
                    {
                        Date = job.Date,
                        JobNo = job.BookingId.ToString(), 
                        Passenger = job.Passenger, 
                        Pickup = job.Pickup, 
                        Destination = job.Destination, 
                        WaitingTime = job.WaitingTime, 
                        Journey = job.PriceAccount, 
                        Parking = job.ParkingCharge,
                        Waiting = job.WaitingPriceAccount,
                        COA = job.COA
                    });
                }

                // get acc details
                var data = await _dB.Accounts
                    .Where(o => o.AccNo == acc.Key)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (acc.Key == 9014 || acc.Key == 10026) // harbour vale - order by passenger name
                {
                    journeys = journeys.OrderBy(o => o.Passenger).ToList();
                }
                else
                {
                    journeys = journeys.OrderBy(o => o.Date).ToList();
                }

                var config = await _dB.CompanyConfig.AsNoTracking().FirstOrDefaultAsync();

                // now generate invoice pdf and send email to acc
                var model = new AccountInvoiceDto
                {
                    InvoiceDate = obj.Date,
                    InvoiceNumber = obj.InvoiceNumber,
                    AccNo = obj.AccountNo.ToString(),
                    CompanyNo = config.CompanyNumber,
                    VatNo = config.VATNumber,

                    Reference = obj.Reference,
                    OrderNo = obj.PurchaseOrderNo,

                    CustomerAddress = new AccountInvoiceDto.Address
                    {
                        BusinessName = data.BusinessName,
                        Address1 = data.Address1,
                        Address2 = data.Address2,
                        Address3 = data.Address3,
                        Address4 = data.Address4,
                        Postcode = data.Postcode
                    },

                    Net = obj.NetTotal,
                    Vat = obj.VatTotal,
                    Total = obj.Total,

                    Items = journeys
                };

                Settings.License = LicenseType.Community;
                var doc = new AccountInvoiceDocument(model);

                if (!Directory.Exists("Data\\Invoices"))
                {
                    Directory.CreateDirectory("Data\\Invoices");
                }

                var fname = $"invoice-{model.InvoiceNumber}.pdf";
                var path = $"Data\\Invoices\\{fname}";
                doc.GeneratePdf(path);

                var bytes = File.ReadAllBytes(path);
                var b64s = Convert.ToBase64String(bytes);

                Stream stream = new MemoryStream(bytes);

                try
                {
                    // save copy to dropbox
                    await _documentService.UploadInvoice(obj.AccountNo, data.BusinessName, stream, fname);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "error uploading document to dropbox");
                }

                pdfs.Add(fname,b64s);

                if (emailInvoices)
                {
                    if (accData.AccNo == 9005 || accData.AccNo == 9006 || accData.AccNo == 90004)
                    {
                        var template = new AccountInvoiceTemplateDto { customer = data.BusinessName, invno = obj.InvoiceNumber.ToString() };
                        await _messagingService.SendAccountInvoiceEmailProDisability(data.Email, obj.Reference, data.BusinessName, template, fname, b64s);
                    }
                    else if (accData.AccNo == 9014 || accData.AccNo == 10026)
                    {
                        var template = new AccountInvoiceTemplateDto { customer = data.BusinessName, invno = obj.InvoiceNumber.ToString() };

                        var attach = new Dictionary<string, string>();
                        attach.Add(fname, b64s);

                        // generate csv
                        var csv = await GenerateInvoiceCSV(model);
                        attach.Add(csv.Filename, csv.Base64);

                        await _messagingService.SendAccountInvoiceAttachmentsEmail(data.Email, data.BusinessName, attach);
                    }
                    else if (accData.AccNo == 10029)
                    {
                        var template = new AccountInvoiceTemplateDto { customer = data.BusinessName, invno = obj.InvoiceNumber.ToString() };

                        await _messagingService.SendAccountInvoiceEmail(data.Email, data.BusinessName, template, fname, b64s);
                        await _messagingService.SendAccountInvoiceEmail("mandydabo@icloud.com", "Mandy Dabo", template, fname, b64s);
                    }
                    else
                    {
                        var template = new AccountInvoiceTemplateDto { customer = data.BusinessName, invno = obj.InvoiceNumber.ToString() };
                        await _messagingService.SendAccountInvoiceEmail(data.Email, data.BusinessName, template, fname, b64s);
                    }
                }
            }

            return pdfs;
        }
        public async Task<List<EarningsModelTotalsDto>> GetDailyEarningsWithinRange(DateTime start, DateTime end, int driver)
        {
            var res = new List<EarningsModelTotalsDto>();

            var vataddedOnCard = await _dB.CompanyConfig.Select(o => o.AddVatOnCardPayments).FirstOrDefaultAsync();

            var data = await _dB.Bookings.Where(o => o.Cancelled == false && o.PickupDateTime >= start.Date && o.PickupDateTime <= end.To2359() && o.UserId == driver)
                  .Select(o => new EarningsModelDto
                  {
                      Date = o.PickupDateTime,
                      UserId = (int)o.UserId,
                      Price = o.Price,
                      WaitingPrice = o.WaitingTimePriceDriver,
                      Scope = (BookingScope)o.Scope,
                      Mileage = o.Mileage
                  }).OrderByDescending(o => o.Date).ToListAsync();


            // group on date
            var grp = data.GroupBy(o => o.Date.Date).ToList();

            foreach (var date in grp)
            {
                var model = new EarningsModelTotalsDto();

                model.Date = date.Key;

                var card = 0.0M;
                if (vataddedOnCard)
                {
                    card = (decimal)date.Where(p => p.Scope == BookingScope.Card).Sum(o => (o.Price / (decimal)1.2));
                }
                else
                {
                    card = (decimal)date.Where(p => p.Scope == BookingScope.Card).Sum(o => o.Price);
                }


                var cash = date.Where(p => p.Scope == BookingScope.Cash).Sum(o => o.Price) + card;
                var acc = date.Where(p => p.Scope == BookingScope.Account).Sum(o => o.Price);
                var rank = date.Where(p => p.Scope == BookingScope.Rank).Sum(o => o.Price);

                var waitingprice = date.Sum(o => o.WaitingPrice);

                // get drivers comms rate
                var cashrate = await _dB.UserProfiles.Where(o => o.UserId == driver).Select(o => o.CashCommissionRate).FirstOrDefaultAsync();
                var rankrate = 7.5;

                model.CashTotal = (double)cash;
                model.RankTotal = (double)rank;
                model.AccTotal = (double)acc;

                model.CashJobsCount = date.Where(o => o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card).Count();
                model.AccJobsCount = date.Where(o => o.Scope == BookingScope.Account).Count();
                model.RankJobsCount = date.Where(o => o.Scope == BookingScope.Rank).Count();

                model.CashMilesCount = date.Where(o => o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card).Sum(o => o.Mileage);
                model.AccMilesCount = date.Where(o => o.Scope == BookingScope.Account).Sum(o => o.Mileage);
                model.RankMilesCount = date.Where(o => o.Scope == BookingScope.Rank).Sum(o => o.Mileage);

                var cashCommission = (model.CashTotal * cashrate / 100);
                var rankCommission = (model.RankTotal * rankrate / 100);

                model.CommsTotal = cashCommission + rankCommission;
                model.GrossTotal = (model.CashTotal + model.RankTotal + model.AccTotal);
                model.NetTotal = (model.CashTotal + model.RankTotal + model.AccTotal) - (cashCommission + rankCommission);

                res.Add(model);
            }

            return res;
        }

        public async Task<List<EarningsModelTotalsDto>> GetEarningsWithinRange(DateTime start, DateTime end, int driver)
        {
            var res = new List<EarningsModelTotalsDto>();

            var data = new List<EarningsModelDto>();

            var vataddedOnCard = await _dB.CompanyConfig.Select(o => o.AddVatOnCardPayments).FirstOrDefaultAsync();

            if (driver == 0)
            {
                data = await _dB.Bookings.Where(o => o.Cancelled == false && (o.PickupDateTime >= start.Date 
                    && o.PickupDateTime <= end.Date.To2359()) && o.UserId != 1)
                  .Select(o => new EarningsModelDto
                  {
                      UserId = o.UserId,
                      Price = o.Price,
                      WaitingPrice = o.WaitingTimePriceDriver,
                      Scope = (BookingScope)o.Scope
                  }).ToListAsync();
            }
            else
            {
                data = await _dB.Bookings.Where(o => o.Cancelled == false && o.PickupDateTime >= start.Date && o.PickupDateTime <= end.To2359() && o.UserId == driver)
                  .Select(o => new EarningsModelDto
                  {
                      UserId = (int)o.UserId,
                      Price = o.Price,
                      WaitingPrice = o.WaitingTimePriceDriver,
                      Scope = (BookingScope)o.Scope, 
                      Mileage = o.Mileage
                  }).ToListAsync();
            }

            data = data.Where(o => o.UserId != null).ToList();

            // group on userid
            var grp = data.GroupBy(o => o.UserId).ToList();

            foreach (var drv in grp)
            {
                var model = new EarningsModelTotalsDto();

                model.UserId = drv.Key.Value;

                var card = 0.0M;
                if (vataddedOnCard)
                {
                    card = (decimal)drv.Where(p => p.Scope == BookingScope.Card).Sum(o => o.Price / (decimal)(1.2));
                }
                else
                {
                    card = (decimal)drv.Where(p => p.Scope == BookingScope.Card).Sum(o => o.Price);
                }


                var cash = drv.Where(p => p.Scope == BookingScope.Cash).Sum(o => o.Price) + card;
                var acc = drv.Where(p => p.Scope == BookingScope.Account).Sum(o => o.Price);
                var rank = drv.Where(p => p.Scope == BookingScope.Rank).Sum(o => o.Price);

                var waitingprice = drv.Sum(o => o.WaitingPrice);

                // get drivers comms rate
                var cashrate = await _dB.UserProfiles.Where(o => o.UserId == drv.Key).Select(o=>o.CashCommissionRate).FirstOrDefaultAsync();
                var rankrate = 7.5;

                model.CashTotal = (double)cash;
                model.RankTotal = (double)rank;
                model.AccTotal = (double)acc;

                model.CashJobsCount = drv.Where(o => o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card).Count();
                model.AccJobsCount = drv.Where(o => o.Scope == BookingScope.Account).Count();
                model.RankJobsCount = drv.Where(o => o.Scope == BookingScope.Rank).Count();

                model.CashMilesCount = drv.Where(o => o.Scope == BookingScope.Cash || o.Scope == BookingScope.Card).Sum(o => o.Mileage);
                model.AccMilesCount = drv.Where(o => o.Scope == BookingScope.Account).Sum(o => o.Mileage);
                model.RankMilesCount = drv.Where(o => o.Scope == BookingScope.Rank).Sum(o => o.Mileage);

                var cashCommission = (model.CashTotal * cashrate / 100);
                var rankCommission = (model.RankTotal * rankrate / 100);

                model.CommsTotal = cashCommission + rankCommission;
                model.GrossTotal = (model.CashTotal + model.RankTotal + model.AccTotal);
                model.NetTotal = (model.CashTotal + model.RankTotal + model.AccTotal) - (cashCommission + rankCommission);

                res.Add(model);
            }

            return res;
        }

        public async Task<List<DriverIdentifer>> GetDriverNameColor()
        {
            var lst = await _dB.UserProfiles.AsNoTracking().Include(o => o.User).Select(o => new DriverIdentifer
            {
                UserId = o.UserId,
                ColorCode = o.ColorCodeRGB,
                FullName = o.User.FullName
            })
                      .AsNoTracking()
                      .ToListAsync();

            return lst;
        }

        public async Task<List<DriverInvoiceStatementDto>> GetStatements(DateTime start, DateTime end, int driver)
        {
            var result = new List<DriverInvoiceStatementDto>();

            List<DriverInvoiceStatement> temp;

            if (driver == 0)
            {
                var data = await _dB.DriverInvoiceStatements.Where(o => 
                  o.DateCreated >= start.Date && o.DateCreated <= end.AddDays(1).Date).OrderBy(o => o.DateCreated)
                  .AsNoTracking()
                  .ToListAsync();

                temp = data;
            }
            else
            {
                var data = await _dB.DriverInvoiceStatements.Where(o => o.UserId == driver &&
                  o.DateCreated >= start.Date && o.DateCreated <= end.AddDays(1).Date).OrderBy(o => o.DateCreated)
                  .AsNoTracking()
                  .ToListAsync();

                temp = data;
            }

            foreach (var statement in temp) 
            {
                List<ChargeableJob> jobs = null;
                try
                {
                    jobs = await _dB.Bookings.Where(o => o.StatementId == statement.StatementId).Include(o => o.Vias)
                    .Select(o => new ChargeableJob
                     {
                         BookingId = o.Id,
                         Cancelled = o.Cancelled,
                         COA = o.CancelledOnArrival,
                         Date = o.PickupDateTime,
                         Destination = o.DestinationAddress,
                         DestinationPostcode = o.DestinationPostCode,
                         Pickup = o.PickupAddress,
                         PickupPostcode = o.PickupPostCode,
                         Passenger = o.PassengerName,
                         Passengers = o.Passengers,
                         Price = (double)o.Price,
                         WaitingMinutes = o.WaitingTimeMinutes,
                         ParkingCharge = (double)o.ParkingCharge,
                         Scope = (BookingScope)o.Scope,
                         UserId = (int)o.UserId,
                         Vias = o.Vias
                     })
                     .ToListAsync();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "error getting statements from database.");
                }

                var dto = _mapper.Map<DriverInvoiceStatement, DriverInvoiceStatementDto>(statement);
                dto.Jobs = jobs.OrderBy(o => o.Date).ToList();

              

                result.Add(dto);
            }

            return result;
        }

        public async Task<List<AccountInvoiceDto>> GetInvoices(DateTime start, DateTime end, int accountNo)
        {
            var result = new List<AccountInvoiceDto>();

            if (accountNo == 0)
            {
                var data = await _dB.AccountInvoices.Where(o =>
                  o.Date.Date >= start.ToUKTime().Date && o.Date.Date <= end.ToUKTime().AddDays(1).Date)
                  .Select(o => new AccountInvoiceDto 
                  {
                      AccNo = o.AccountNo.ToString(),
                      InvoiceDate = o.Date,
                      InvoiceNumber = o.InvoiceNumber,
                      Net = o.NetTotal, Vat = o.VatTotal,
                      OrderNo = o.PurchaseOrderNo, 
                      Reference = o.Reference, 
                      Paid = o.Paid,
                      Total = o.Total
                  })
                  .AsNoTracking()
                  .ToListAsync();

                result = data;
            }
            else
            {
                var startdate = start.ToUKTime().Date;
                var enddate = end.AddDays(1);

                var data = await _dB.AccountInvoices.Where(o => o.AccountNo == accountNo &&
                  o.Date.Date >= startdate && o.Date <= enddate.Date)
                    .Select(o => new AccountInvoiceDto
                    {
                        AccNo = o.AccountNo.ToString(),
                        InvoiceDate = o.Date,
                        InvoiceNumber = o.InvoiceNumber,
                        Net = o.NetTotal,
                        Vat = o.VatTotal,
                        OrderNo = o.PurchaseOrderNo,
                        Paid = o.Paid,
                        Reference = o.Reference,
                        Total = o.Total
                    })
                  .AsNoTracking()
                  .ToListAsync();

                result = data;
            }

            foreach (var invoice in result)
            {
                var jobs = await _dB.Bookings.Where(o => o.InvoiceNumber == invoice.InvoiceNumber)
                    .Select(o => new JourneyItem
                    {
                        JobNo = o.Id.ToString(),
                        COA = o.CancelledOnArrival,
                        Date = o.PickupDateTime,
                        Destination = o.DestinationAddress + ", " + o.DestinationPostCode,
                        Pickup = o.PickupAddress + ", " + o.PickupPostCode,
                        Passenger = o.PassengerName,
                        WaitingTime = o.WaitingTimeMinutes.ToString(),
                        Waiting = (double)o.WaitingTimePriceAccount,
                        Parking = (double)o.ParkingCharge,
                        Journey = (int)o.PriceAccount,
                    })
                    .ToListAsync();
                
                jobs = jobs.OrderBy(o => o.Date).ToList();
                invoice.Items = jobs;
            }

            return result;
        }

        public async Task<AccountInvoiceDto> GetInvoice(int invoiceNumber)
        {
            var result = await _dB.AccountInvoices.Where(o =>
               o.InvoiceNumber == invoiceNumber)
               .Select(o => new AccountInvoiceDto
               {
                   AccNo = o.AccountNo.ToString(),
                   InvoiceDate = o.Date,
                   InvoiceNumber = o.InvoiceNumber,
                   Net = o.NetTotal,
                   Vat = o.VatTotal,
                   OrderNo = o.PurchaseOrderNo,
                   Reference = o.Reference,
                   Paid = o.Paid,
                   Total = o.Total
               })
               .AsNoTracking()
               .FirstOrDefaultAsync();

            var accno = Convert.ToInt32(result.AccNo);
            var customer = await _dB.Accounts.Where(o=>o.AccNo == accno).FirstOrDefaultAsync();

            result.CustomerAddress = new AccountInvoiceDto.Address();
            result.CustomerAddress.BusinessName = customer.BusinessName;
            result.CustomerAddress.Address1 = customer.Address1;
            result.CustomerAddress.Address2 = customer.Address2;
            result.CustomerAddress.Address3 = customer.Address3;
            result.CustomerAddress.Address4 = customer.Address4;
            result.CustomerAddress.Postcode = customer.Postcode;

            var jobs = await _dB.Bookings.Where(o => o.InvoiceNumber == result.InvoiceNumber)
                  .Select(o => new JourneyItem
                  {
                      JobNo = o.Id.ToString(),
                      COA = o.CancelledOnArrival,
                      Date = o.PickupDateTime,
                      Destination = o.DestinationAddress + ", " + o.DestinationPostCode,
                      Pickup = o.PickupAddress + ", " + o.PickupPostCode,
                      Passenger = o.PassengerName,
                      WaitingTime = o.WaitingTimeMinutes.ToString(),
                      Waiting = (double)o.WaitingTimePriceAccount,
                      Parking = (double)o.ParkingCharge,
                      Journey = (int)o.PriceAccount,
                  })
                  .ToListAsync();

            jobs = jobs.OrderBy(o => o.Date).ToList();
            result.Items = jobs;

            return result;
        }

        public async Task<DriverInvoiceStatementDto?> GetStatementById(int statementId)
        {
            var result = new List<DriverInvoiceStatementDto>();

            var data = await _dB.DriverInvoiceStatements.Where(o => o.StatementId == statementId)
                .AsNoTracking()
                .ToListAsync();

            List<DriverInvoiceStatement> temp;

            temp = data;

            foreach (var statement in temp)
            {
                List<ChargeableJob> jobs = null;
                try
                {
                    jobs = await _dB.Bookings.Where(o => o.StatementId == statement.StatementId).Include(o => o.Vias)
                    .Select(o => new ChargeableJob
                    {
                        BookingId = o.Id,
                        Cancelled = o.Cancelled,
                        COA = o.CancelledOnArrival,
                        Date = o.PickupDateTime,
                        Destination = o.DestinationAddress,
                        DestinationPostcode = o.DestinationPostCode,
                        Pickup = o.PickupAddress,
                        PickupPostcode = o.PickupPostCode,
                        Passenger = o.PassengerName,
                        Passengers = o.Passengers,
                        Price = (double)o.Price,
                        WaitingMinutes = o.WaitingTimeMinutes,
                        ParkingCharge = (double)o.ParkingCharge,
                        Scope = (BookingScope)o.Scope,
                        UserId = (int)o.UserId,
                        Vias = o.Vias
                    })
                     .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "error getting statements from database.");
                }


                // check for vat
                var chargeVatOnCard = await _dB.CompanyConfig.Select(o => o.AddVatOnCardPayments).FirstOrDefaultAsync();

                if (chargeVatOnCard)
                {
                    foreach (var job in jobs.Where(o => o.Scope == BookingScope.Card))
                    {
                        // recalc price to remove vat
                        job.Price = Math.Round((job.Price / 1.2),2);
                    }
                }

                var dto = _mapper.Map<DriverInvoiceStatement, DriverInvoiceStatementDto>(statement);
                dto.Jobs = jobs.OrderBy(o => o.Date).ToList();

                result.Add(dto);
            }
                
            return result.First();
        }

        public async Task GenerateSendCreditNotePDF(int invoiceNumber, string reason)
        {
            var model = await GetInvoice(invoiceNumber);

            if (!Directory.Exists("Data\\CreditNotes"))
            {
                Directory.CreateDirectory("Data\\CreditNotes");
            }

            // insert credit note details
            var credit = new CreditNote();
            credit.InvoiceDate = model.InvoiceDate;
            credit.InvoiceNumber = invoiceNumber;
            credit.NumberOfJourneys = model.Items.Count;
            credit.Date = DateTime.Now.ToUKTime();
            credit.VatTotal = model.Vat;
            credit.AccountNo = model.AccNo;
            credit.NetTotal = model.Net;
            credit.Reason = reason;
            credit.Total = model.Total;

            await _dB.CreditNotes.AddAsync(credit);
            await _dB.SaveChangesAsync();

            Settings.License = LicenseType.Community;

            var date = DateTime.Now.ToUKTime();
            var doc = new AccountCreditNoteDocument(model,reason,credit.Id,date);

            var fname = $"credit-note-{credit.Id}.pdf";
            var path = $"Data\\CreditNotes\\{fname}";
            doc.GeneratePdf(path);

            var bytes = File.ReadAllBytes(path);
            var b64s = Convert.ToBase64String(bytes);

            Stream stream = new MemoryStream(bytes);

            var accno = Convert.ToInt32(model.AccNo);

            var accData = _dB.Accounts.Where(o => o.AccNo == accno).Select(o => new { o.BusinessName, o.Email, o.ContactName }).FirstOrDefault();

            try
            {
                // save copy to dropbox
                await _documentService.UploadCreditNote(accno, accData.BusinessName, stream, fname);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error uploading document to dropbox");
            }

            await _messagingService.SendAccountCreditNoteEmail(accData.Email, accData.ContactName, fname, b64s);
        }

        public async Task GenerateSendCreditNotePDF(int invoiceNumber, string reason, int[] bookingIds)
        {
            var model = await GetInvoice(invoiceNumber);

            if (!Directory.Exists("Data\\CreditNotes"))
            {
                Directory.CreateDirectory("Data\\CreditNotes");
            }

            // get only requested bookings
            var jobs = model.Items.Where(o => bookingIds.Contains(Convert.ToInt32(o.JobNo))).ToList();

            if (jobs.Count == 0)
                throw new InvalidDataException("There were no jobs found that match the sent booking ids.");

            model.Items.Clear();
            model.Items.AddRange(jobs);

            // insert credit note details
            var credit = new CreditNote();
            credit.InvoiceDate = model.InvoiceDate;
            credit.InvoiceNumber = invoiceNumber;
            credit.NumberOfJourneys = jobs.Count;
            credit.Date = DateTime.Now.ToUKTime();
            credit.AccountNo = model.AccNo;
            credit.Reason = reason;

            var net = jobs.Sum(o => o.Total);
            var totalInc = jobs.Sum(o => o.TotalInc);
            var vat = totalInc - net;

            credit.NetTotal = (decimal)net;
            credit.VatTotal = (decimal)vat;
            credit.Total = (decimal)totalInc;

            model.Total = credit.Total;
            model.Vat = credit.VatTotal;
            model.Net = credit.NetTotal;

            await _dB.CreditNotes.AddAsync(credit);
            await _dB.SaveChangesAsync();

            var config = await _dB.CompanyConfig.AsNoTracking().FirstOrDefaultAsync();

            model.CompanyNo = config.CompanyNumber;
            model.VatNo = config.VATNumber;

            Settings.License = LicenseType.Community;

            var date = DateTime.Now.ToUKTime();
            var doc = new AccountCreditNoteDocument(model, reason, credit.Id, date);

            var fname = $"credit-note-{credit.Id}.pdf";
            var path = $"Data\\CreditNotes\\{fname}";
            doc.GeneratePdf(path);

            var bytes = File.ReadAllBytes(path);
            var b64s = Convert.ToBase64String(bytes);

            Stream stream = new MemoryStream(bytes);

            var accno = Convert.ToInt32(model.AccNo);

            var accData = _dB.Accounts.Where(o => o.AccNo == accno).Select(o => new { o.BusinessName, o.Email, o.ContactName }).FirstOrDefault();

            try
            {
                // save copy to dropbox
                await _documentService.UploadCreditNote(accno, accData.BusinessName, stream, fname);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error uploading document to dropbox");
            }

            await _messagingService.SendAccountCreditNoteEmail(accData.Email, accData.ContactName, fname, b64s);
        }

        public async Task<List<CreditNote>> GetCreditNotes(int accno)
        {
            var credits = new List<CreditNote>();

            if(accno != 0)
                credits = await _dB.CreditNotes.Where(o => o.AccountNo == accno.ToString()).ToListAsync();
            else
                credits = await _dB.CreditNotes.ToListAsync();

            return credits;
        }

        /// <summary>
        /// Creates a PDF statement
        /// </summary>
        /// <param name="list"></param>
        /// <returns>filename of the PDF</returns>
        public async Task<List<(string Filename,string Base64)>> GenerateStatementPDF(List<DriverInvoiceStatementDto> list)
        {
            Settings.License = LicenseType.Community;
            var res = new List<(string Filename, string Base64)>();
            foreach (var item in list)
            {
                // get profile
                var profile = _dB.UserProfiles
                    .Include(o => o.User)
                    .AsNoTracking()
                    .Where(o => o.UserId == item.UserId)
                    .Select(o=> new { o.UserId, o.RegNo, o.VehicleMake, o.VehicleModel, o.VehicleColour, o.VehicleType, o.User.FullName})
                    .FirstOrDefault();

                var doc = new DriverStatementDocument(item, profile);

                if (!Directory.Exists("Data\\Statements"))
                {
                    Directory.CreateDirectory("Data\\Statements");
                }

                var fname = $"statement-{item.StatementId}.pdf";
                var path = $"Data\\Statements\\{fname}";

                var upload = false;
               // if (!File.Exists(path))
                { 
                    doc.GeneratePdf(path); 
                    upload = true;
                }

                var bytes = File.ReadAllBytes(path);
                var b64s = Convert.ToBase64String(bytes);

                Stream stream = new MemoryStream(bytes);

                if (upload)
                {
                    try
                    {
                        // save copy to dropbox
                        await _documentService.UploadStatement(profile.UserId, profile.FullName, stream, fname);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "error uploading document to dropbox");
                    }
                    
                }

                res.Add(new (fname,b64s));
            }

            return res;
        }

        public async Task<(string Filename, string Base64)> GenerateInvoiceCSV(AccountInvoiceDto invoice)
        {
            var fname = $"invoice-{invoice.InvoiceNumber}.csv";
            var path = $"Data\\Invoices\\{fname}";

            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }

            if (!Directory.Exists("Data\\Invoices"))
            {
                Directory.CreateDirectory("Data\\Invoices");
            }

            if (!File.Exists(path))
            {
                var sb = new StringBuilder();

                sb.AppendLine("Job #,Date,Passenger,COA,Pickup,Destination,Parking,Waiting Time,Waiting Charge,Journey,Total,Total Inc Vat");

                foreach (var item in invoice.Items)
                {
                    sb.AppendLine($"\"{item.JobNo}\",\"{item.Date}\",\"{item.Passenger}\",\"{item.COA}\",\"{item.Pickup}\",\"{item.Destination}\",\"{item.Parking}\",\"{item.WaitingTime}\",\"{item.Waiting}\",\"{item.Journey}\",\"{item.Total}\",\"{item.TotalInc}\"");
                }

                File.WriteAllText(path, sb.ToString());
            }

            var bytes = File.ReadAllBytes(path);
            var b64s = Convert.ToBase64String(bytes);

            return (fname, b64s);
        }

        public async Task<(string Filename, string Base64)> GenerateInvoiceCSV(int invoiceNumber)
        {
            var fname = $"invoice-{invoiceNumber}.csv";
            var path = $"Data\\Invoices\\{fname}";

            if (!File.Exists(path))
            {
                var obj = await GetInvoice(invoiceNumber);

                var sb = new StringBuilder();

                sb.AppendLine("Job #,Date,Passenger,COA,Pickup,Destination,Parking,Waiting Time,Waiting Charge,Journey,Total,Total Inc Vat");

                foreach (var item in obj.Items)
                {
                    sb.AppendLine($"\"{item.JobNo}\",\"{item.Date}\",\"{item.Passenger}\",\"{item.COA}\",\"{item.Pickup}\",\"{item.Destination}\",\"{item.Parking}\",\"{item.WaitingTime}\",\"{item.Waiting}\",\"{item.Journey}\",\"{item.Total}\",\"{item.TotalInc}\"");
                }

                File.WriteAllText(path, sb.ToString());
            }

            var bytes = File.ReadAllBytes(path);
            var b64s = Convert.ToBase64String(bytes);

            return (path, b64s);
        }

        public async Task ResendDriverStatement(int statementId)
        {
            var statement = await GetStatementById(statementId);

            if (statement != null)
            {
                // get email address and fullname
                var user = await _dB.Users.Where(o => o.Id == statement.UserId).Select(o => new { o.FullName, o.Email }).FirstOrDefaultAsync();

                // get pdf 
                var res = await GenerateStatementPDF(new List<DriverInvoiceStatementDto> {statement});

                await _messagingService.SendDriverStatementResendEmail(user.Email, user.FullName, res.First().Filename, res.First().Base64);
            }
        }

        public async Task ResendAccountInvoice(int invoiceNumber)
        {
            var fname = $"invoice-{invoiceNumber}.pdf";
            var path = $"Data\\Invoices\\{fname}";

            // get required data
            var accno = await _dB.AccountInvoices.Where(o=>o.InvoiceNumber == invoiceNumber)
                .Select(o=>o.AccountNo).FirstOrDefaultAsync();

            var data = await _dB.Accounts.Where(o => o.AccNo == accno)
                .Select(o => new { o.BusinessName, o.Email, o.Reference })
                .FirstOrDefaultAsync();

            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                var b64s = Convert.ToBase64String(bytes);

                Stream stream = new MemoryStream(bytes);

                if (accno == 9005 || accno == 9006 || accno == 90004)
                {
                    var template = new AccountInvoiceTemplateDto { customer = data.BusinessName, invno = invoiceNumber.ToString() };
                    await _messagingService.SendAccountInvoiceEmailProDisability(data.Email, data.Reference, data.BusinessName, template, fname, b64s);
                }
                else
                {
                    var template = new AccountInvoiceTemplateDto { customer = data.BusinessName, invno = invoiceNumber.ToString() };
                    await _messagingService.SendAccountInvoiceEmail(data.Email, data.BusinessName, template, fname, b64s);
                }
            }
            else // create PDF
            {
                throw new NotImplementedException("This function has not been implemented.");
            }
        }


        public async Task MarkStatementPaid(int statmentId)
        { 
            await _dB.DriverInvoiceStatements.Where(o=>o.StatementId == statmentId)
                .ExecuteUpdateAsync(b => b.SetProperty(u => u.PaidInFull, true));
        }

        public async Task MarkInvoicePaid(int invoiceNo)
        {
            await _dB.AccountInvoices.Where(o => o.InvoiceNumber == invoiceNo)
                .ExecuteUpdateAsync(b => b.SetProperty(u => u.Paid, true));
        }

        public async Task PriceBookingByMileage(UpdateBookingQuoteRequestDto dto)
        {
            GetPriceResponseDto priceData;

            priceData = await _tariffService.Get9999CashPrice(dto.PickupDateTime, dto.Passengers,
                dto.PickupPostcode, dto.DestinationPostcode, dto.ViaPostcodes, dto.PriceFromBase);


            await _dB.Bookings.Where(o => o.Id == dto.BookingId).ExecuteUpdateAsync(o => 
                o.SetProperty(u => u.ActionByUserId,dto.ActionByUserId)
                .SetProperty(u => u.UpdatedByName, dto.UpdatedByName)
                .SetProperty(u => u.Price, (decimal)priceData.PriceAccount)
                .SetProperty(u => u.Mileage, dto.Mileage)
                .SetProperty(u => u.MileageText, dto.MileageText)
                .SetProperty(u => u.DurationText, dto.DurationText)
                .SetProperty(u => u.ChargeFromBase, dto.PriceFromBase)
                .SetProperty(u => u.ManuallyPriced,false));

            dto.Price = (decimal)priceData.PriceAccount;
        }

        public async Task<decimal> PriceBookingByMileageAccount(UpdateBookingQuoteRequestDto dto)
        {
            GetPriceResponseDto priceData;

            priceData = await _tariffService.Get9999CashPrice(dto.PickupDateTime, dto.Passengers,
                dto.PickupPostcode, dto.DestinationPostcode, dto.ViaPostcodes, dto.PriceFromBase);


            await _dB.Bookings.Where(o => o.Id == dto.BookingId).ExecuteUpdateAsync(o =>
                o.SetProperty(u => u.ActionByUserId, dto.ActionByUserId)
                .SetProperty(u => u.UpdatedByName, dto.UpdatedByName)
                .SetProperty(u => u.PriceAccount, (decimal)priceData.PriceAccount)
                .SetProperty(u => u.Mileage, dto.Mileage)
                .SetProperty(u => u.MileageText, dto.MileageText)
                .SetProperty(u => u.DurationText, dto.DurationText)
                .SetProperty(u => u.ChargeFromBase, dto.PriceFromBase)
                .SetProperty(u => u.ManuallyPriced, false));

            dto.PriceAccount = (decimal)priceData.PriceAccount;

            return dto.PriceAccount.Value;
        }

        public async Task<Dictionary<int, GetPriceResponseDto>> UpdatePricesBulk(UpdateBookingQuoteBulkRequestDto dto)
        {
            var res = new Dictionary<int, GetPriceResponseDto>();

            var bookingId = dto.BookingIds.Min();

            // get last journey with matching postcodes that was invoiced
            var found = await _dB.Bookings.Where(o => o.Scope == BookingScope.Account && o.Id != bookingId &&
             o.AccountNumber == dto.AccountNo &&
             o.PickupPostCode == dto.PickupPostcode.Replace("  ", " ") &&
             o.DestinationPostCode == dto.DestinationPostcode.Replace("  ", " ") &&
             o.PriceAccount > 0 && o.PickupDateTime.Date < dto.PickupDateTime.Date && o.Cancelled == false)
             .AsNoTracking()
             .OrderByDescending(o => o.PickupDateTime)
             .Take(5)
             .Select(o => new
             {
                 o.Id,
                 o.PriceAccount,
                 PriceDriver = o.Price,
                 o.Mileage,
                 o.MileageText,
                 o.DurationMinutes,
                 o.DurationText,
                 o.ChargeFromBase,
                 o.Passengers,
                 o.PassengerName,
                 o.PickupDateTime
             })
             .FirstOrDefaultAsync();

           // found = null;

            if (found == null) // no previous price
            {
                var hvsPrice = await _tariffService.GetOnInvoicePrices(dto.PickupPostcode, dto.DestinationPostcode, dto.ViaPostcodes, dto.AccountNo);

                var price = (decimal)Math.Round(hvsPrice.PriceAccount, 2);
                var dprice = (decimal)Math.Round(hvsPrice.PriceDriver, 2);
                var miles = (decimal)Math.Round(hvsPrice.TotalMileage, 1);

                foreach (var id in dto.BookingIds)
                {
                    await _dB.Bookings.Where(o => o.Id == id)
                    .ExecuteUpdateAsync(o =>
                    o.SetProperty(u => u.PriceAccount, price)
                    .SetProperty(u => u.Mileage, miles)
                    .SetProperty(u => u.MileageText, hvsPrice.MileageText)
                    .SetProperty(u => u.DurationText, hvsPrice.DurationText)
                    .SetProperty(u => u.ChargeFromBase, true));

                    // only update driver price if not on a statement
                    await _dB.Bookings.Where(o => o.Id == id && o.StatementId == null)
                        .ExecuteUpdateAsync(o => o.SetProperty(u => u.Price, dprice));

                    hvsPrice.PriceReference = "TARIFF";
                    hvsPrice.PriceDriver = (double)dprice;
                    res.Add(id, hvsPrice);
                }

                return res;
            }
            else // previous price found
            {
                var miles = (double?)found.Mileage == null ? -1 : (double)found.Mileage;

                    foreach (var id in dto.BookingIds)
                    {
                        await _dB.Bookings.Where(o => o.Id == id)
                          .ExecuteUpdateAsync(o =>
                          o.SetProperty(u => u.PriceAccount, found.PriceAccount)
                          .SetProperty(u => u.Mileage, found.Mileage)
                          .SetProperty(u => u.MileageText, found.MileageText)
                          .SetProperty(u => u.DurationText, found.DurationText)
                          .SetProperty(u => u.ChargeFromBase, true));

                    await _dB.Bookings.Where(o => o.Id == id && o.StatementId == null)
                        .ExecuteUpdateAsync(o => o.SetProperty(u => u.Price, found.PriceDriver));

                    res.Add(id, new GetPriceResponseDto
                        {
                            PriceAccount = (double)found.PriceAccount,
                            JourneyMileage = miles,
                            PriceDriver = (double)found.PriceDriver,
                            PriceReference = $"JOB MATCH (=) {found.Id} : {found.PickupDateTime:dd/MM/yy HH:mm} - {found.PassengerName}"
                        });
                    }

                return res;
            }
        }


        public async Task<object> Test()
        {
            // Use midnight boundary to keep it sargable
            var cutoff = new DateTime(2025,09,09);

            //
            var pc = "DT9 4DN".Replace("  ", " ");
            var dc = "SP8 4PH".Replace("  ", " ");
            var bid = new List<int> { 112495, 112269, 112270, 112271, 112504, 112275, 120586, 112510, 112494 };
            var bookingId = bid.Min();

            var top5 = await _dB.Bookings
               .Where(o =>
                   o.Scope == BookingScope.Account &&
                   o.Id != bookingId &&
                   o.AccountNumber == 9014 &&
                   o.PickupPostCode == pc &&
                   o.DestinationPostCode == dc &&
                   o.PriceAccount > 0 &&
                   !o.Cancelled &&
                   o.PickupDateTime < cutoff)
               .AsQueryable()
               .OrderByDescending(o => o.PickupDateTime)
               .ThenByDescending(o => o.Id)
               .Select(o => new { o.Id, o.PickupDateTime, o.PassengerName })
               //.Take(5)
               .ToListAsync();

            return top5;
        }

        #region HVS
        /// <summary>
        /// Gets both Account & Driver prices for HVS bookings and updates the booking.
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<Dictionary<int, GetPriceResponseDto>> UpdatePricesHVSBulk(UpdateBookingQuoteBulkRequestDto dto)
        {
            var res = new Dictionary<int, GetPriceResponseDto>();

            // 3) Exclude ALL current bookings
            var excludeIds = (dto.BookingIds ?? Array.Empty<int>()).Distinct().ToArray();

            // Use midnight boundary to keep it sargable
            var cutoff = dto.PickupDateTime.Date;

            //
            var pc = dto.PickupPostcode.Replace("  ", " ");
            var dc = dto.DestinationPostcode.Replace("  ", " ");

            // get last journey with matching postcodes that was invoiced
            var founds = await _dB.Bookings.Where(o => o.Scope == BookingScope.Account &&
            !excludeIds.Contains(o.Id) &&
             o.AccountNumber == 9014 &&
             o.PickupPostCode == pc &&
             o.DestinationPostCode == dc &&
             o.PriceAccount > 0 &&
             (o.PickupDateTime > new DateTime(2025,09,01,0,0,0) && // only look back 1 months
             o.PickupDateTime < cutoff) &&
             o.Cancelled == false)
             .AsNoTracking()
             .OrderByDescending(o => o.PickupDateTime)
             //.Take(5)
             .ThenByDescending(o => o.Id)
             .Select(o => new
             {
                 o.Id,
                 o.PriceAccount,
                 PriceDriver = o.Price,
                 o.Mileage,
                 o.MileageText,
                 o.DurationMinutes,
                 o.DurationText,
                 o.ChargeFromBase,
                 o.Passengers,
                 o.PassengerName,
                 o.PickupDateTime
             })
             .ToListAsync();

            var found = founds.FirstOrDefault();

            found = null;

            if (found == null) // no previous price
            {
                var jrnys = await _dB.Bookings
                    .Include(o=>o.Vias)
                    .Where(o => dto.BookingIds.Contains(o.Id))
                    .Select(o => new { o.Id,o.Passengers, o.Vias })
                    .ToListAsync();

                foreach (var j in jrnys)
                {
                    var vias = j.Vias.Select(o => o.PostCode).ToList();

                    var hvsPrice = await _tariffService.GetPriceHVS(dto.PickupPostcode, dto.DestinationPostcode, vias);

                    var price = (decimal)Math.Round(hvsPrice.PriceAccount, 2);
                    var dprice = (decimal)Math.Round(hvsPrice.PriceDriver, 2);
                    var miles = (decimal)Math.Round(hvsPrice.TotalMileage, 1);

                    await _dB.Bookings.Where(o => o.Id == j.Id)
                   .ExecuteUpdateAsync(o =>
                   o.SetProperty(u => u.PriceAccount, price)
                   .SetProperty(u => u.Mileage, miles)
                   .SetProperty(u => u.MileageText, hvsPrice.MileageText)
                   .SetProperty(u => u.DurationText, hvsPrice.DurationText)
                   .SetProperty(u => u.ChargeFromBase, true));

                    // only update driver price if not on a statement
                    await _dB.Bookings.Where(o => o.Id == j.Id && o.StatementId == null)
                        .ExecuteUpdateAsync(o => o.SetProperty(u => u.Price, dprice));

                    hvsPrice.PriceReference = "TARIFF";
                    hvsPrice.PriceDriver = (double)dprice;
                    res.Add(j.Id, hvsPrice);
                }

                return res;
            }
            else // previous price found
            {
                var miles = (double?)found.Mileage == null ? -1 : (double)found.Mileage;

                // NEW
                var jobdata = new List<dynamic>();

                // iterate over each booking
                foreach (var item in dto.BookingIds)
                {
                    var data = await _dB.Bookings.Where(o => o.Id == item)
                        .Select(o => new { o.Id, o.Passengers })
                        .FirstOrDefaultAsync();
                    jobdata.Add(data);
                }

                foreach (var job in jobdata)
                {
                    var id = (int)job.Id;

                    if (job.Passengers == found.Passengers)
                    {
                        await _dB.Bookings.Where(o => o.Id == id)
                        .ExecuteUpdateAsync(o =>
                        o.SetProperty(u => u.PriceAccount, found.PriceAccount)
                        .SetProperty(u => u.Mileage, found.Mileage)
                        .SetProperty(u => u.MileageText, found.MileageText)
                        .SetProperty(u => u.DurationText, found.DurationText)
                        .SetProperty(u => u.ChargeFromBase, true));

                        // only update driver price if not on a statement
                        await _dB.Bookings.Where(o => o.Id == id && o.StatementId == null)
                            .ExecuteUpdateAsync(o => o.SetProperty(u => u.Price, found.PriceDriver));

                        res.Add(id, new GetPriceResponseDto
                        {
                            PriceAccount = (double)found.PriceAccount,
                            JourneyMileage = miles,
                            PriceDriver = (double)found.PriceDriver,
                            PriceReference = $"JOB MATCH (=) {found.Id} : {found.PickupDateTime:dd/MM/yy HH:mm} - {found.PassengerName}"
                        });
                    }
                    else if (found.Passengers < job.Passengers)
                    {
                        
                        var cnt = ((int)job.Passengers - found.Passengers);
                        var price = found.PriceAccount + (cnt * 15);
                        var dprice = found.PriceDriver + (cnt * 7);

                        await _dB.Bookings.Where(o => o.Id == id)
                         .ExecuteUpdateAsync(o =>
                         o.SetProperty(u => u.PriceAccount, price)
                         .SetProperty(u => u.Mileage, found.Mileage)
                         .SetProperty(u => u.MileageText, found.MileageText)
                         .SetProperty(u => u.DurationText, found.DurationText)
                         .SetProperty(u => u.ChargeFromBase, true));

                        // only update driver price if not on a statement
                        await _dB.Bookings.Where(o => o.Id == id && o.StatementId == null)
                            .ExecuteUpdateAsync(o => o.SetProperty(u => u.Price, dprice));

                        res.Add(id, new GetPriceResponseDto
                        {
                            PriceAccount = (double)price,
                            JourneyMileage = miles,
                            PriceDriver = (double)dprice,
                            PriceReference = $"JOB MATCH (<) {found.Id} : {found.PickupDateTime:dd/MM/yy HH:mm} - {found.PassengerName}"
                        });

                    }
                    else if(found.Passengers > job.Passengers)
                    {
                        var cnt = 0;
                        var price = 0m;
                        var dprice = 0m;
                        if (found.Passengers > 1 && dto.Passengers == 1)
                        {
                            cnt = found.Passengers - 1;
                            price = found.PriceAccount - (cnt * 15);
                            dprice = found.PriceDriver - (cnt * 7);
                        }
                        else
                        {
                            cnt = found.Passengers - dto.Passengers;
                            price = found.PriceAccount - (cnt * 15);
                            dprice = found.PriceDriver - (cnt * 7);
                        }

                        await _dB.Bookings.Where(o => o.Id == id)
                            .ExecuteUpdateAsync(o =>
                            o.SetProperty(u => u.PriceAccount, price)
                            .SetProperty(u => u.Mileage, found.Mileage)
                            .SetProperty(u => u.MileageText, found.MileageText)
                            .SetProperty(u => u.DurationText, found.DurationText)
                            .SetProperty(u => u.ChargeFromBase, true));

                        // only update driver price if not on a statement
                        await _dB.Bookings.Where(o => o.Id == id && o.StatementId == null)
                            .ExecuteUpdateAsync(o => o.SetProperty(u => u.Price, dprice));

                        res.Add(id, new GetPriceResponseDto
                        {
                            PriceAccount = (double)price,
                            JourneyMileage = miles,
                            PriceDriver = (double)dprice,
                            PriceReference = $"JOB MATCH (>) {found.Id} : {found.PickupDateTime:dd/MM/yy HH:mm} - {found.PassengerName}"
                        });
                    }
                }

                // NEW

                return res;
            }
        }

        /// <summary>
        /// Updates the account price only
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<GetPriceResponseDto> UpdateGetAccPriceHVS(UpdateBookingQuoteRequestDto dto)
        {
            GetPriceResponseDto priceData;

            var found = await _dB.Bookings.Where(o => o.Scope == BookingScope.Account && o.Id != dto.BookingId &&
                o.PickupPostCode == dto.PickupPostcode.Replace("  ", " ") &&
                o.DestinationPostCode == dto.DestinationPostcode.Replace("  ", " ") &&
                o.PriceAccount > 0 && o.PickupDateTime.Date <= dto.PickupDateTime.Date && o.Cancelled == false)
                .AsNoTracking()
                .OrderByDescending(o => o.PickupDateTime)
                .Take(5)
                .Select(o => new
                {
                    o.Id,
                    o.PriceAccount,
                    o.Mileage,
                    o.MileageText,
                    o.DurationMinutes,
                    o.DurationText,
                    o.ChargeFromBase,
                    o.Passengers,
                    o.PassengerName,
                    o.PickupDateTime
                })
                .FirstOrDefaultAsync();


            if (found == null) // no previous price
            {
                priceData = await _tariffService.GetPriceHVS(dto.PickupPostcode, dto.DestinationPostcode, dto.ViaPostcodes);

                var price = (decimal)Math.Round(priceData.PriceAccount, 2);
                var miles = (decimal)Math.Round(priceData.TotalMileage, 1);

                await _dB.Bookings.Where(o => o.Id == dto.BookingId)
                    .ExecuteUpdateAsync(o =>
                    o.SetProperty(u => u.PriceAccount, price)
                    .SetProperty(u => u.Mileage, miles)
                    .SetProperty(u => u.MileageText, "Unavailable")
                    .SetProperty(u => u.DurationText, priceData.DurationText)
                    .SetProperty(u => u.ChargeFromBase, true));

                return priceData;
            }
            else // previous journey found
            {
                var miles = (double?)found.Mileage == null ? -1 : (double)found.Mileage;
                if (dto.Passengers == found.Passengers)
                {
                    await _dB.Bookings.Where(o => o.Id == dto.BookingId)
                      .ExecuteUpdateAsync(o =>
                      o.SetProperty(u => u.PriceAccount, found.PriceAccount)
                      .SetProperty(u => u.Mileage, found.Mileage)
                      .SetProperty(u => u.MileageText, "Unavailable")
                      .SetProperty(u => u.DurationText, found.DurationText)
                      .SetProperty(u => u.ChargeFromBase, true));
                }
                else
                {
                    // fix price
                    if (found.Passengers < dto.Passengers) // less passengers on found booking 
                    {
                        var cnt = dto.Passengers - found.Passengers;
                        var price = found.PriceAccount + (cnt * 15);

                        await _dB.Bookings.Where(o => o.Id == dto.BookingId)
                            .ExecuteUpdateAsync(o =>
                            o.SetProperty(u => u.PriceAccount, price)
                            .SetProperty(u => u.Mileage, found.Mileage)
                            .SetProperty(u => u.MileageText, "Unavailable")
                            .SetProperty(u => u.DurationText, found.DurationText)
                            .SetProperty(u => u.ChargeFromBase, true));

                        return new GetPriceResponseDto { PriceAccount = (double)price, JourneyMileage = miles };
                    }
                    else // more passengers on found
                    {
                        if (found.PickupDateTime.Date > new DateTime(2025, 05, 7).Date)
                        {
                            var cnt = found.Passengers - dto.Passengers;
                            var price = found.PriceAccount - (cnt * 15);

                            await _dB.Bookings.Where(o => o.Id == dto.BookingId)
                                .ExecuteUpdateAsync(o =>
                                o.SetProperty(u => u.PriceAccount, price)
                                .SetProperty(u => u.Mileage, found.Mileage)
                                .SetProperty(u => u.MileageText, "Unavailable")
                                .SetProperty(u => u.DurationText, found.DurationText)
                                .SetProperty(u => u.ChargeFromBase, true));

                            return new GetPriceResponseDto { PriceAccount = (double)price, JourneyMileage = miles };
                        }

                    }
                }

               //return new GetPriceResponseDto { TotalPrice = (double)found.PriceAccount, JourneyMileage = miles };
                return new GetPriceResponseDto { PriceAccount = (double)-88, JourneyMileage = 0 };
            }
        }
        #endregion


        public async Task<Result> ManualPriceUpdate(ManualPriceUpdateRequestDto request)
        {
            try
            {
                await _dB.Bookings.Where(o => o.Id == request.BookingId).ExecuteUpdateAsync(o =>
                o.SetProperty(u => u.ActionByUserId, request.ActionByUserId)
               .SetProperty(u => u.UpdatedByName, request.UpdatedByName)
               .SetProperty(u => u.Price, request.Price)
               .SetProperty(u => u.Mileage, 0)
               .SetProperty(u => u.MileageText, "")
               .SetProperty(u => u.DurationText, "")
               .SetProperty(u => u.ChargeFromBase, false)
               .SetProperty(u => u.ManuallyPriced, true));

                _logger.LogInformation($"Price for booking number {request.BookingId} manually updated");

                return Result.Ok();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"Error manually updating price.");
                return Result.Fail("Error updating price, there was an error thrown.");
            }

        }

        public async Task<Result> ManualPriceAccountUpdate(ManualPriceUpdateRequestDto request)
        {
            try
            {
                await _dB.Bookings.Where(o => o.Id == request.BookingId).ExecuteUpdateAsync(o =>
                o.SetProperty(u => u.ActionByUserId, request.ActionByUserId)
               .SetProperty(u => u.UpdatedByName, request.UpdatedByName)
               .SetProperty(u => u.PriceAccount, request.PriceAccount.Value)
               .SetProperty(u => u.Mileage, 0)
               .SetProperty(u => u.MileageText, "")
               .SetProperty(u => u.DurationText, "")
               .SetProperty(u => u.ChargeFromBase, false)
               .SetProperty(u => u.ManuallyPriced, true));

                _logger.LogInformation($"AccountPrice for booking number {request.BookingId} manually updated");

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manually updating price.");
                return Result.Fail("Error updating price, there was an error thrown.");
            }

        }

        public async Task<AccountProcessingRowTotalsDto> UpdateChargesDataAcc(int bookingId,int waiting, double parking, double journey, double driver)
        {
            var obj = new AccountProcessingRowTotalsDto();

            var wcharge = CalculateAccountWaitingPrice(waiting);

            await _dB.Bookings.Where(o=>o.Id == bookingId)
                .ExecuteUpdateAsync(o => 
                o.SetProperty(u => u.WaitingTimeMinutes, waiting)
                .SetProperty(u => u.WaitingTimePriceAccount, wcharge)
                .SetProperty(u=>u.ParkingCharge,(decimal)parking)
                .SetProperty(u => u.PriceAccount, (decimal)journey)
                .SetProperty(u => u.Price, (decimal)driver));

            obj.WaitMinutes = waiting;
            obj.WaitCharge = (double)wcharge;
            obj.Parking = parking;
            obj.AccountPrice = journey;
            obj.DriverPrice = driver;

            return obj;
        }

        public async Task<DriverProcessingRowTotalDto> UpdateChargesDataDriver(int bookingId, int waiting, double parking, double driver)
        {
            var obj = new DriverProcessingRowTotalDto();

            var wcharge = CalculateDriverWaitingPrice(waiting);

            await _dB.Bookings.Where(o => o.Id == bookingId)
                .ExecuteUpdateAsync(o =>
                o.SetProperty(u => u.WaitingTimeMinutes, waiting)
                .SetProperty(u => u.WaitingTimePriceDriver, wcharge)
                .SetProperty(u => u.ParkingCharge, (decimal)parking)
                .SetProperty(u => u.Price, (decimal)driver));

            obj.WaitMinutes = waiting;
            obj.WaitCharge = (double)wcharge;
            obj.Parking = parking;
            obj.Price = driver;

            return obj;
        }

        public async Task<List<Account>> GetAllAccounts()
        {
            var data = await _dB.Accounts.Where(o => o.Deleted == false).AsNoTracking().ToListAsync();
            var lst = new List<string>();

            var accnos = data.Select(o => o.AccNo.ToString()).ToList();

            var details = await _dB.Users.Where(o => accnos.Contains(o.UserName)).Select(o => new { o.UserName, o.FullName, o.Email }).ToListAsync();

            foreach (var acc in data)
            {
                var det = details.FirstOrDefault(o => o.UserName == acc.AccNo.ToString());
                if (det != null)
                {
                    acc.BookerName = det.FullName;
                    acc.BookerEmail = det.Email;
                }
            }

            return data;
        }

        public async Task<Account?> GetAccount(int accountNo)
        {
            return await _dB.Accounts.FirstOrDefaultAsync(o => o.AccNo == accountNo);
        }

        public async Task<int> CreateAccount(AccountDto account)
        {
            var obj = _mapper.Map<AccountDto, Account>(account);

            var accno = await CreateAccount(obj);

            return accno.Value;
        }

        public async Task<Result<int>> CreateAccount(Account request)
        {
            var exists = await _dB.Accounts.AnyAsync(o => o.AccNo == request.AccNo);

            if (!exists)
            {
                await _dB.Accounts.AddAsync(request);
                await _dB.SaveChangesAsync();
                return Result.Ok(request.AccNo);
            }
            else
            {
                throw new Exception($"The Account with Number {request.AccNo} already exists.");
            }
        }

        public async Task UpdateAccount(AccountDto account)
        {
            var obj = _mapper.Map<AccountDto, Account>(account);
            await UpdateAccount(obj);
        }

        public async Task<Result> UpdateAccount(Account request)
        {
            var acc = await _dB.Accounts.Where(o => o.AccNo == request.AccNo).FirstOrDefaultAsync();

            if (acc != null)
            {
                acc.ContactName = request.ContactName;
                acc.BusinessName = request.BusinessName;
                acc.Address1 = request.Address1;
                acc.Address2 = request.Address2;
                acc.Address3 = request.Address3;
                acc.Address4 = request.Address4;
                acc.Postcode = request.Postcode;    
                acc.Telephone = request.Telephone;  
                acc.PurchaseOrderNo = request.PurchaseOrderNo;
                acc.Reference = request.Reference;
                acc.Email = request.Email;
                acc.AccountTariffId = request.AccountTariffId;
                await _dB.SaveChangesAsync();
                return Result.Ok();
            }
            else
            {
                throw new Exception($"The Account with Number {request.AccNo} was not found.");
            }
        }
        public async Task DeleteAccount(int accountNo) 
        {

            await _dB.Accounts.Where(o => o.AccNo == accountNo)
                .ExecuteUpdateAsync(b => b.SetProperty(u => u.Deleted, true));
        }
        public async Task DeleteAll()
        {
            await _dB.Accounts.ExecuteDeleteAsync();
        }

        public decimal CalculateDriverWaitingPrice(int minutes)
        {
            var permin = 0.33;
            var sum = minutes * permin;
            return (decimal)sum;
        }

        public decimal CalculateAccountWaitingPrice(int minutes)
        {
            var permin = 0.42;
            var sum = minutes * permin;
            return (decimal)sum;
        }

        public async Task<int> ImportCsv(string filepath)
        {
            var lines = await File.ReadAllLinesAsync(filepath);
            var accs = new List<Account>();

            for (int i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split(",");

                // Identity Key
                var accNumber = fields[0].Trim();

                var contactName = fields[1].Trim();
                var businessName = fields[2].Trim();
                var address1 = fields[3].Trim();
                var address2 = fields[4].Trim();
                var address3 = fields[5].Trim();
                var address4 = fields[6].Trim();
                var postcode = fields[7].Trim();
                var tel = fields[8].Trim();
                var email = fields[9].Trim();

                accs.Add(new Account
                {
                    ContactName = contactName,
                    BusinessName = businessName,
                    Address1 = address1,
                    Address2 = address2,
                    Address3 = address3,
                    Address4 = address4,
                    Postcode = postcode,
                    Email = email,
                    Telephone = tel,
                });
            }

            _dB.Accounts.AddRange(accs);
            await _dB.SaveChangesAsync();
            return accs.Count;
        }

        public async Task PostJob(int bookingId, bool post)
        {
            // check for 0 price jobs and skip posting
            var price = await _dB.Bookings.Where(o => o.Id == bookingId).Select(o => o.PriceAccount).FirstOrDefaultAsync();

            if (price == 0 && post == true)
                return;

            await _dB.Bookings.Where(o=>o.Id == bookingId).ExecuteUpdateAsync(o => o.SetProperty(u => u.PostedForInvoicing, post));
        }

        public async Task PostJobDriver(int bookingId, bool post)
        {
            await _dB.Bookings.Where(o => o.Id == bookingId).ExecuteUpdateAsync(o => o.SetProperty(u => u.PostedForStatement, post));
        }

        public record VatOutputRecord
        {
            public BookingScope Scope { get; set; }
            public int UserId { get; set; }
            public decimal Price { get; set; }
            public decimal VatAmount { get; set; }
        }

        public async Task<string> CalculateVatOutputs(DateTime from, DateTime to)
        {
            var cardvat = await _dB.CompanyConfig.Select(o => o.AddVatOnCardPayments).FirstOrDefaultAsync();

            var data = await _dB.Bookings.Where(o => (o.Cancelled == false) &&
                (o.Scope == BookingScope.Cash || o.Scope == BookingScope.Rank || o.Scope == BookingScope.Card) &&
                (o.PickupDateTime.Date >= from.Date && o.PickupDateTime.Date <= to.Date) &&
                (o.UserId != null))
                .AsNoTracking()
                .Select(o => new VatOutputRecord{ UserId = (int)o.UserId, Price = o.Price, Scope = (BookingScope)o.Scope, VatAmount = o.VatAmountAdded })
                .ToListAsync();

            var grp = data.GroupBy(o => o.Scope).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("SCOPE,TOTAL CASH,COMMISSION TAKEN,VAT TOTAL");

            // get commission rates for drivers
            var driverIds = data.Select(o => o.UserId).Distinct().ToList();
            var commsRates = await _dB.UserProfiles.Select(o => new { o.UserId, o.CashCommissionRate }).Where(o => driverIds.Contains(o.UserId)).ToListAsync();

            // ------------------------

            // loop over scope
            foreach (var scope in grp)
            {
                var grp2 = scope.GroupBy(o => o.UserId).ToList();

                var valueTotal = 0M;
                var commsTotal = 0M;
                var vatTotal = 0M;

                foreach (var usergrp in grp2)
                {
                    var lst = usergrp.ToList();
                    var uid = usergrp.Key;

                    if (scope.Key == BookingScope.Card)
                    {
                        if (cardvat)
                        {
                            // adjust card prices for removal of vat if applicable
                            foreach (var item in lst.Where(o => o.Scope == BookingScope.Card))
                            {
                                item.Price = item.Price - item.VatAmount;
                            }
                        }
                    }
                    var total = lst.Sum(o => o.Price);
                    // get drivers commission rate
                    var rate = commsRates
                        .Where(o => o.UserId == uid)
                        .Select(o => o.CashCommissionRate)
                        .FirstOrDefault();

                    var comms = total * (rate / 100M);  // X % of total 
                    var vat = (comms / 100) * 20;

                    valueTotal+= total;
                    commsTotal+= comms;
                    vatTotal+= vat;
                }

                sb.AppendLine($"{scope.Key},{Math.Round(valueTotal, 2)},{Math.Round(commsTotal, 2)},{Math.Round(vatTotal, 2)}");
            }

            // if we are charging vat on card - calculate amount here
            if (cardvat)
            {
                var data1 = await _dB.Bookings.Where(o => (o.Cancelled == false) && o.Scope == BookingScope.Card &&
                    (o.PickupDateTime.Date >= from.Date && o.PickupDateTime.Date <= to.Date) &&
                    (o.UserId != null) && o.VatAmountAdded > 0)
                    .AsNoTracking()
                    .Select(o => new { o.Price, o.VatAmountAdded }).ToListAsync();

                var totalPrice = data1.Sum(o => o.Price);
                var totalVat = data1.Sum(o => o.VatAmountAdded);

                sb.AppendLine($"CARD PAYMENTS VAT, {Math.Round(totalPrice, 2)},0,{Math.Round(totalVat,2)}");
            }

            return sb.ToString();
        }

        public async Task CreditInvoice(int invoiceNo, string reason)
        {
            await GenerateSendCreditNotePDF(invoiceNo, reason);

            await _dB.Bookings.Where(o => o.InvoiceNumber == invoiceNo)
              .ExecuteUpdateAsync(o => o.SetProperty(u => u.InvoiceNumber, (int?)null));

            await _dB.AccountInvoices.Where(o => o.InvoiceNumber == invoiceNo).ExecuteDeleteAsync();
        }


        public async Task<AccountTariff> CreateOrUpdateAccountTariff(AccountTariff tariff)
        {
            if (tariff.Id == 0) // create
            {
                await _dB.AccountTariffs.AddAsync(tariff);
            }
            else // update
            {
                _dB.AccountTariffs.Update(tariff);
            }

            await _dB.SaveChangesAsync();

            return tariff;
        }

        public async Task<List<AccountTariff>> GetAccountTariffs()
        {
            var lst = await _dB.AccountTariffs
                .ToListAsync();

            return lst;
        }


    }
}



