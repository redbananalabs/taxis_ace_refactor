using TaxiDispatch.Data;
using TaxiDispatch.Data.Models;
using TaxiDispatch.DTOs.Booking;
using Amazon.Runtime.Internal.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.Intrinsics.Arm;

namespace TaxiDispatch.Services
{
    public class TariffService : BaseService<TariffService>
    {
        private const string BasePostcode = "SP8 4PZ";
        private readonly string _distanceMatrixApiKey;

        public TariffService(
            TaxiDispatchContext dB,
            ILogger<TariffService> logger,
            IConfiguration configuration)
            : base(dB, logger)
        {
            _distanceMatrixApiKey = configuration["Google:DistanceMatrixApiKey"]
                ?? configuration["Google:PlacesApiKey"]
                ?? throw new InvalidOperationException("Google:DistanceMatrixApiKey missing");
        }

        public async Task<List<Tariff>> GetAllTariffs()
        { 
            return await _dB.Tariffs.AsNoTracking().ToListAsync();
        }

        public async Task Update(Tariff obj)
        {
            _dB.Tariffs.Update(obj);
            await _dB.SaveChangesAsync();
        }

        public async Task<GetPriceResponseDto> GetPriceHVS(string pickupPostcode, string destinationPostcode, List<string>? viaPostcodes)
        {
            if (viaPostcodes == null)
                viaPostcodes = new List<string>();

            // total miles of journey
            var journeys = new List<JourneyDetails>();

            var ab = await GetDrivingDistance(pickupPostcode, destinationPostcode);
            var ba = await GetDrivingDistance(destinationPostcode, pickupPostcode);

            var avgJourney = new JourneyDetails
            {
                Miles = (ab.Miles + ba.Miles) / 2,
                Minutes = (ab.Minutes + ba.Minutes) / 2,
                MileageText = $"{((ab.Miles + ba.Miles) / 2):N1} miles",
                DurationText = $"{((ab.Minutes + ba.Minutes) / 2)} mins",
                StartPostcode = pickupPostcode,
                EndPostcode = destinationPostcode
            };

            journeys.Add(avgJourney);

            ///

            //if (viaPostcodes.Count == 0) // a - b journey
            //{
            //    var ab = await GetDrivingDistance(pickupPostcode, destinationPostcode);
            //    var ba = await GetDrivingDistance(destinationPostcode, pickupPostcode);

            //    var avgJourney = new JourneyDetails
            //    {
            //        Miles = (ab.Miles + ba.Miles) / 2,
            //        Minutes = (ab.Minutes + ba.Minutes) / 2,
            //        MileageText = $"{((ab.Miles + ba.Miles) / 2):N1} miles",
            //        DurationText = $"{((ab.Minutes + ba.Minutes) / 2)} mins",
            //        StartPostcode = pickupPostcode,
            //        EndPostcode = destinationPostcode
            //    };

            //    journeys.Add(avgJourney);

            //}
            //else // has vias
            //{
            //    // number of vias
            //    var count = viaPostcodes.Count;

            //    // calculate all segements of the journey
            //    for (var i = 0; i < viaPostcodes.Count; i++)
            //    {
            //        if (count == 1) // get distance from pickup to via[0] then destination
            //        {
            //            // pickup to via
            //            journeys.Add(await GetDrivingDistance(pickupPostcode, viaPostcodes[i]));
            //            // via to destination
            //            journeys.Add(await GetDrivingDistance(viaPostcodes[i], destinationPostcode));
            //        }
            //        else // more than 1 via
            //        {
            //            if (i == 0) // pickup to first via
            //            {
            //                // pickup to via
            //                journeys.Add(await GetDrivingDistance(pickupPostcode, viaPostcodes[i]));
            //            }
            //            else if (i < count) // previous via to this via 
            //            {
            //                journeys.Add(await GetDrivingDistance(viaPostcodes[i - 1], viaPostcodes[i]));

            //                if ((i + 1) == count) // final via to destination
            //                {
            //                    journeys.Add(await GetDrivingDistance(viaPostcodes[i], destinationPostcode));
            //                }
            //            }
            //        }
            //    }
            //}

            // calculate dead mileage
            var deadMiles = 0.0;
            var deadMinutes = 0;

            var fromBase = true;

            if (fromBase)
            {
                var lega = await GetDrivingDistance(BasePostcode, pickupPostcode);
                var legc = await GetDrivingDistance(destinationPostcode, BasePostcode);

                var lega1 = await GetDrivingDistance(BasePostcode, destinationPostcode);
                var legc1 = await GetDrivingDistance(pickupPostcode, BasePostcode);

                //deadMiles = lega.Miles + legc.Miles;
                deadMiles = ((lega.Miles + lega1.Miles) / 2) + ((legc.Miles + legc1.Miles) / 2);
                deadMinutes = legc.Minutes;
            }
            else
            {
                var b2b = await GetDrivingDistance(destinationPostcode, BasePostcode);
                deadMinutes = b2b.Minutes;
            }

            // calculate total miles of full journey
            var journeyMiles = Math.Round(journeys.Sum(x => Math.Round(x.Miles, 2)), 2);

            // calculate total minutes of full journey
            var journeyMinutes = journeys.Sum(x => x.Minutes);

            // calculate from static tariff
            var res = new GetPriceResponseDto();

            // calculate response data
            res.FromBase = true;
            res.DeadMileage = deadMiles;
            res.DeadMinutes = deadMinutes;
            res.JourneyMileage = journeyMiles;
            res.JourneyMinutes = journeyMinutes;

            // report number of legs
            res.Legs = journeys.Count + 2;

            // driver price
            var miles = (journeyMiles + deadMiles) / 2;
            res.Tariff = "Harbour Vale";

            res.PriceDriver = Math.Round(miles * 2.40, 2);

            // minus 15%
            res.PriceDriver *= (double)0.85M;

            // add 7 per via
            foreach (var via in viaPostcodes)
            {
                if (via != "DT9 4DN")
                    res.PriceDriver += 7;
            }

            // account price
            res.PriceAccount = Math.Round(miles * 2.60, 2);

            // add 15 per via
            foreach (var via in viaPostcodes)
            {
                if (via != "DT9 4DN")
                    res.PriceAccount += 15;
            }

            return res;
        }


        /// <summary>
        /// Gets the price for the journey from pickup to destination including any stops on route to destination
        /// </summary>
        /// <param name="dateTime">Pickup Time - Date</param>
        /// <param name="passengers">Number of passengers</param>
        /// <param name="pickupPostcode">Pickup Postcode</param>
        /// <param name="destinationPostcode">destination Postcode</param>
        /// <param name="viaPostcodes">Postcodes of any stops before destination</param>
        /// <param name="fromBase">true if pricing from base postcode</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<GetPriceResponseDto> Get9999CashPrice(DateTime dateTime,int passengers, string pickupPostcode, 
            string destinationPostcode, List<string>? viaPostcodes, bool fromBase)
        {
            if(viaPostcodes == null)
                viaPostcodes = new List<string>();

            // total miles of journey
            var journeys = new List<JourneyDetails>();
            
            if (viaPostcodes.Count == 0) // a - b journey
            {
                journeys.Add(await GetDrivingDistance(pickupPostcode, destinationPostcode));
            }
            else // has vias
            {
                // number of vias
                var count = viaPostcodes.Count;

                // calculate all segements of the journey
                for (var i = 0; i < viaPostcodes.Count; i++) 
                {
                    if (count == 1) // get distance from pickup to via[0] then destination
                    {
                        // pickup to via
                        journeys.Add(await GetDrivingDistance(pickupPostcode, viaPostcodes[i]));
                        // via to destination
                        journeys.Add(await GetDrivingDistance(viaPostcodes[i], destinationPostcode));
                    }
                    else // more than 1 via
                    {
                        if(i == 0) // pickup to first via
                        {
                            // pickup to via
                            journeys.Add(await GetDrivingDistance(pickupPostcode, viaPostcodes[i]));
                        }
                        else if(i < count) // previous via to this via 
                        {
                            journeys.Add(await GetDrivingDistance(viaPostcodes[i - 1], viaPostcodes[i]));

                            if((i+1) == count) // final via to destination
                            {
                                journeys.Add(await GetDrivingDistance(viaPostcodes[i], destinationPostcode));
                            }
                        }
                    }
                }
            }


            // calculate dead mileage
            var deadMiles = 0.0;
            var deadMinutes = 0;

            fromBase = true;

            if (fromBase)
            {
                var lega = await GetDrivingDistance(BasePostcode, pickupPostcode);
                var legc = await GetDrivingDistance(destinationPostcode, BasePostcode);

                deadMiles = lega.Miles + legc.Miles;
                deadMinutes = legc.Minutes;
            }
            else
            {
                var b2b = await GetDrivingDistance(destinationPostcode, BasePostcode);
                deadMinutes = b2b.Minutes;
            }

            // calculate total miles of full journey
            var journeyMiles = Math.Round(journeys.Sum(x => x.Miles),2);

            // calculate total minutes of full journey
            var journeyMinutes = journeys.Sum(x => x.Minutes);

            // get the mileage price and tariff
            //var res = await GetCashPrice(dateTime, passengers, journeyMiles, deadMiles);

            var tariff = await GetTariff(dateTime);

            if (tariff == null)
            {
                throw new ArgumentNullException(nameof(tariff));
            }

            // create response object with calculated tariff name
            var res = new GetPriceResponseDto { Tariff = tariff.Name };

            // calculate any standing charge and a minimun of 1 mile
            var price = tariff.InitialCharge + tariff.FirstMileCharge;

            // total journey miles
            var totalMiles = 0.0;

            if (deadMiles > 0) // from base
            {
                totalMiles = ((journeyMiles + deadMiles) / 2);
            }
            else
                totalMiles = (journeyMiles + deadMiles);

            if (totalMiles > 1)
            {
                // calculate per mile rate minus first mile as we have calculated above
                var journeyPrice = (totalMiles - 1) * tariff.AdditionalMileCharge;

                // add to baseline charge
                price += journeyPrice;
            }
            else // min fare?
            {
                price = 5.00; // minimun fare
            }

            // 5 seater + - update the price by 50%
            if (passengers > 4)
            {
                price = price + (price / 2); // add 50% more
            }

            res.PriceAccount = Math.Round(Math.Ceiling(price), 2);
            res.PriceDriver = res.PriceAccount; // same price for driver

            // calculate response data
            res.FromBase = fromBase;
            res.DeadMileage = deadMiles;
            res.DeadMinutes = deadMinutes;
            res.JourneyMileage = journeyMiles;
            res.JourneyMinutes = journeyMinutes;

            // report number of legs
            if (fromBase)
                res.Legs = journeys.Count + 2;
            else
                res.Legs = journeys.Count;

            return res;
        }

   

        /// <summary>
        /// Gets prices on all other accounts accept 9999 | 9014 | 10026
        /// </summary>
        /// <param name="pickupPostcode"></param>
        /// <param name="destinationPostcode"></param>
        /// <param name="viaPostcodes"></param>
        /// <param name="accno"></param>
        /// <returns></returns>
        public async Task<GetPriceResponseDto> GetOnInvoicePrices(string pickupPostcode, string destinationPostcode, List<string>? viaPostcodes, int accno)
        {
            if (viaPostcodes == null)
                viaPostcodes = new List<string>();

            // total miles of journey
            var journeys = new List<JourneyDetails>();

            if (viaPostcodes.Count == 0) // a - b journey
            {
                journeys.Add(await GetDrivingDistance(pickupPostcode, destinationPostcode));
            }
            else // has vias
            {
                // number of vias
                var count = viaPostcodes.Count;

                // calculate all segements of the journey
                for (var i = 0; i < viaPostcodes.Count; i++)
                {
                    if (count == 1) // get distance from pickup to via[0] then destination
                    {
                        // pickup to via
                        journeys.Add(await GetDrivingDistance(pickupPostcode, viaPostcodes[i]));
                        // via to destination
                        journeys.Add(await GetDrivingDistance(viaPostcodes[i], destinationPostcode));
                    }
                    else // more than 1 via
                    {
                        if (i == 0) // pickup to first via
                        {
                            // pickup to via
                            journeys.Add(await GetDrivingDistance(pickupPostcode, viaPostcodes[i]));
                        }
                        else if (i < count) // previous via to this via 
                        {
                            journeys.Add(await GetDrivingDistance(viaPostcodes[i - 1], viaPostcodes[i]));

                            if ((i + 1) == count) // final via to destination
                            {
                                journeys.Add(await GetDrivingDistance(viaPostcodes[i], destinationPostcode));
                            }
                        }
                    }
                }
            }


            // calculate dead mileage
            var deadMiles = 0.0;
            var deadMinutes = 0;

            var fromBase = true;

            if (fromBase)
            {
                var lega = await GetDrivingDistance(BasePostcode, pickupPostcode);
                var legc = await GetDrivingDistance(destinationPostcode, BasePostcode);

                deadMiles = lega.Miles + legc.Miles;
                deadMinutes = legc.Minutes;
            }
            else
            {
                var b2b = await GetDrivingDistance(destinationPostcode, BasePostcode);
                deadMinutes = b2b.Minutes;
            }

            // calculate total miles of full journey
            var journeyMiles = Math.Round(journeys.Sum(x => x.Miles), 2);

            // calculate total minutes of full journey
            var journeyMinutes = journeys.Sum(x => x.Minutes);


            // get account tariffId and tariff
            var tariffId = await _dB.Accounts.Where(o => o.AccNo == accno).Select(o => o.AccountTariffId).FirstOrDefaultAsync();
            var tariff = await _dB.AccountTariffs.Where(o => o.Id == tariffId).FirstOrDefaultAsync();

         
            // calculate from static tariff
            var res = new GetPriceResponseDto();

            // calculate response data
            res.FromBase = true;
            res.DeadMileage = deadMiles;
            res.DeadMinutes = deadMinutes;
            res.JourneyMileage = journeyMiles;
            res.JourneyMinutes = journeyMinutes;

            // report number of legs
            res.Legs = journeys.Count + 2;

            var miles = (journeyMiles + deadMiles) / 2;
            res.Tariff = tariff.Name;

            // calculate price based on tariff
            var price = tariff.DriverInitialCharge + tariff.DriverFirstMileCharge;

            // calculate per mile rate minus first mile as we have calculated above
            var journeyPrice = (miles - 1) * tariff.DriverAdditionalMileCharge;

            // driver price journey
            price += journeyPrice;
            res.PriceDriver = Math.Round(price, 2);

            // calculate price based on tariff
            var aprice = 0.0;

            aprice = tariff.AccountInitialCharge + tariff.AccountFirstMileCharge;

            // calculate per mile rate minus first mile as we have calculated above
            var accountJourneyPrice = (miles - 1) * tariff.AccountAdditionalMileCharge;


            // account price journey
            aprice += Math.Round(accountJourneyPrice);
            res.PriceAccount = Math.Round(Math.Ceiling(aprice), 2);

            return res;
        }

        public async Task<JourneyDetails> GetDrivingDistance(string originPostcode, string destinationPostcode)
        {
            string requestUrl =
                $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={originPostcode}&destinations={destinationPostcode}&key={_distanceMatrixApiKey}";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Parse the JSON response to extract the driving distance
                    var details = ParseDrivingDistance(responseBody);

                    // include the start & end postcodes
                    details.StartPostcode = originPostcode;
                    details.EndPostcode = destinationPostcode;
                    return details;
                }
                else
                {
                    // Handle the error response
                    throw new Exception($"Error occurred while requesting distance matrix: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

        private JourneyDetails ParseDrivingDistance(string jsonResponse)
        {
            MapsResponse response = null;

            try
            {
                response = Newtonsoft.Json.JsonConvert.DeserializeObject<MapsResponse>(jsonResponse);
            }
            catch (NullReferenceException ex)
            {
                throw new NullReferenceException($"Unable to get directions, possible invalid postcode.",ex);
            }

            double drivingDistance = (double)response.rows[0].elements[0].distance.value; // null ref error occurces
            
            int durationValue = response.rows[0].elements[0].duration.value;
            int durationMinutes = durationValue / 60;
            string duration = response.rows[0].elements[0].duration.text;

            // Convert the distance value from meters to kilometers
            drivingDistance /= 1000;

            double miles = drivingDistance / 1.6;

            var journey = new JourneyDetails 
            { 
                MileageText = $"{miles:N1} miles", 
                DurationText = duration, 
                Miles = miles, 
                Minutes = durationMinutes 
            };

            return journey;
        }

        private async Task<Tariff> GetTariff(DateTime dateTime)
        {
            Tariff? tariff = null;

            // check for special day
            if (dateTime.Date.Month == 12)
            {
                if ((dateTime.Date.Day == 24 && dateTime.Hour >= 18)  // xmas eve
                    || (dateTime.Date.Day == 31 && dateTime.Hour >= 18)) // new year eve
                {
                    // tariff 3
                    tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_3).FirstOrDefaultAsync();
                }
                else if (dateTime.Date.Day == 25 || // xmas day
                    dateTime.Date.Day == 26) // boxing day
                {
                    // tariff 3
                    tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_3).FirstOrDefaultAsync();
                }
                else if (dateTime.Date.Day == 24 && dateTime.Hour < 18)
                {
                    // tariff 1
                    tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_1).FirstOrDefaultAsync();
                }
                else
                {
                    if (dateTime.DayOfWeek == DayOfWeek.Saturday && (dateTime.Hour < 22 && dateTime.Hour >= 7))
                    {
                        // tariff 1
                        tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_1).FirstOrDefaultAsync();
                    }
                    else if (dateTime.DayOfWeek == DayOfWeek.Saturday && dateTime.Hour < 7)
                    {
                        // tariff 2
                        tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
                    }
                    else if (dateTime.DayOfWeek == DayOfWeek.Sunday || // sunday
                        (dateTime.DayOfWeek == DayOfWeek.Monday && dateTime.Hour < 7) || IsBankHoliday(dateTime)) // monday < 7am
                    {
                        // tariff 2
                        tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
                    }
                    else
                    {
                        if (dateTime.Hour >= 22)
                        {
                            // tariff 2
                            tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
                        }
                        else if (dateTime.Hour < 7)
                        {
                            // tariff 2
                            tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
                        }
                        else
                        {
                            // tariff 1
                            tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_1).FirstOrDefaultAsync();
                        }
                    }

                    //if (dateTime.Hour == 22 || dateTime.Hour == 23 || dateTime.Hour < 7)
                    //{
                    //    // tariff 2
                    //    tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
                    //}
                    //else
                    //{
                    //    // tariff 1
                    //    tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_1).FirstOrDefaultAsync();
                    //}
                }
            }
            else if (dateTime.Month == 1 && dateTime.Day == 1) // new years day
            {
                // tariff 3
                tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_3).FirstOrDefaultAsync();
            }
            else if (dateTime.DayOfWeek == DayOfWeek.Saturday && dateTime.Hour > 22)
            {
                // tariff 2
                tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
            }
            else if (dateTime.DayOfWeek == DayOfWeek.Sunday || // sunday
                (dateTime.DayOfWeek == DayOfWeek.Monday && dateTime.Hour < 7) || IsBankHoliday(dateTime)) // monday < 7am
            {
                // tariff 2
                tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
            }
            else
            {
                if (dateTime.Hour >= 22)
                {
                    // tariff 2
                    tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
                }
                else if (dateTime.Hour < 7)
                {
                    // tariff 2
                    tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_2).FirstOrDefaultAsync();
                }
                else
                {
                    // tariff 1
                    tariff = await _dB.Tariffs.Where(o => o.Type == Domain.TariffType.Tariff_1).FirstOrDefaultAsync();
                }
            }

            return tariff;
        }

        private bool IsBankHoliday(DateTime dateTime)
        {
            var holidays = new Dictionary<DateTime, string>();
            holidays.Add(new DateTime(2025, 4, 18), "Good Friday");
            holidays.Add(new DateTime(2025, 4, 21), "Easter Monday");
            holidays.Add(new DateTime(2025, 5, 5), "Early May Bank Holiday");
            holidays.Add(new DateTime(2025, 5, 26), "Spring Bank Holiday");
            holidays.Add(new DateTime(2025, 8, 25), "Summer Bank Holiday");

            holidays.Add(new DateTime(2026, 4, 3), "Good Friday");
            holidays.Add(new DateTime(2026, 4, 6), "Easter Monday");
            holidays.Add(new DateTime(2026, 5, 4), "Early May Bank Holiday");
            holidays.Add(new DateTime(2026, 5, 25), "Spring Bank Holiday");
            holidays.Add(new DateTime(2026, 8, 31), "Summer Bank Holiday");


            holidays.Add(new DateTime(2027, 3, 26), "Good Friday");
            holidays.Add(new DateTime(2027, 3, 29), "Easter Monday");
            holidays.Add(new DateTime(2027, 5, 3), "Early May Bank Holiday");
            holidays.Add(new DateTime(2027, 5, 31), "Spring Bank Holiday");
            holidays.Add(new DateTime(2027, 8, 30), "Summer Bank Holiday");

            return holidays.ContainsKey(dateTime.Date);
        }


        #region Google JSON response
        public class Distance
        {
            public string text { get; set; }
            public int value { get; set; }
        }

        public class Duration
        {
            public string text { get; set; }
            public int value { get; set; }
        }

        public class Element
        {
            public Distance distance { get; set; }
            public Duration duration { get; set; }
            public string status { get; set; }
        }

        public class MapsResponse
        {
            public List<string> destination_addresses { get; set; }
            public List<string> origin_addresses { get; set; }
            public List<Row> rows { get; set; }
            public string status { get; set; }
        }

        public class Row
        {
            public List<Element> elements { get; set; }
        }
        #endregion
    }
}


