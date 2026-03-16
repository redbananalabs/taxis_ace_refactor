using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using System.Collections.ObjectModel;

namespace TaxiDispatch.Data
{
    public class MockData
    {
        public ObservableCollection<Booking> GetMockBookingsForInsert()
        {
            ObservableCollection<Booking> bookings = new ObservableCollection<Booking>();

            var dates = new List<DateTime>() { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) };
            var counter = 0;
            foreach (var date in dates)
            {
                for (int i = 0; i < 30; i++)
                {
                    counter++;

                    
                    
                    ConfirmationStatus confirmed = ConfirmationStatus.Confirmed;
                    var recur = string.Empty;


                    var start = GetRandomTimeToday(date);

                        recur = GetRandomRecurrenceRule(start.Minute);

                    bookings.Add(new Booking
                    {
                        Id = counter,

                        // Subject = $"{RandomString(10 + i)}",

                        PickupAddress = $"{RandomString(10 + i)}",
                        PickupPostCode = RandomString(9),
                        PickupDateTime = start,

                        Details = "These are details",

                        //BackgroundColourRGB = $"{random.Next(0, 255)},{random.Next(0, 255)},{random.Next(0, 255)}",

                        PassengerName = RandomString(7),
                        Email = RandomString(5) + "@" + RandomString(6) + ".com",
                        PhoneNumber = RandomPhone(),

                        BookedByName = RandomString(i + 5),
                        ConfirmationStatus = confirmed,
                        
                        RecurrenceRule = recur,
                        DateCreated = DateTime.Now,
                    });
                }
            }

            return bookings;
        }

        private Random random = new Random();

        private DateTime GetRandomTimeToday(DateTime date)
        {
            var dd = date.Day;
            var mm = date.Month;
            var yy = 2023;

            var hh = random.Next(0, 23);
            var min = random.Next(0, 30);

            return new DateTime(yy, mm, dd, hh, min, 0);
        }

        private DateTime GetEndTime(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute + 30, 0);
        }

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private string RandomPhone()
        {
            var num = "07" + random.Next(100000000, 999999999);
            return num;
        }

        private ConfirmationStatus GetRandomStatus(PaymentStatus status)
        {
            if (status == PaymentStatus.Paid)
                return ConfirmationStatus.Confirmed;
            else
            {
                var rand = random.Next(1, 100);
                var res = rand < 50 ? 0 : 1;
                return (ConfirmationStatus)res;
            }
        }

        private PaymentStatus GetRandomPaymentStatus()
        {
            var rand = random.Next(1, 100);
            var res = rand < 50 ? 2 : 3;

            return (PaymentStatus)res;
        }

        private string GetRandomRecurrenceRule(int minutes)
        {
            var r1 = "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO;COUNT=10";
            var r2 = "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;INTERVAL=1";
            var r3 = "FREQ=WEEKLY;INTERVAL=1;BYDAY=FR";
            var r4 = "FREQ=MONTHLY;BYMONTHDAY=20;INTERVAL=1;COUNT=5";
            var r5 = "";

            if (minutes < 10)
                return r1;
            else if (minutes > 10 && minutes < 20)
                return r2;
            else if (minutes > 10 && minutes < 20)
                return r3;
            else if (minutes > 20 && minutes < 30)
                return r4;
            else if (minutes > 40 && minutes < 50)
                return r5;

            return r5;
        }


        // Tariffs
        public List<Tariff> GetTariffsForInsert()
        {
            var list = new List<Tariff>
            {
                new Tariff
                {
                    Id = 1,
                    Type = TariffType.Tariff_1,
                    Name = "Tariff 1 : Day Rate",
                    Description = "Chargeable from 7am until 10pm.",
                    InitialCharge = 3.00,
                    FirstMileCharge = 4.40,
                    AdditionalMileCharge = 2.80
                },

                new Tariff
                {
                    Id = 2,
                    Type = TariffType.Tariff_2,
                    Name = "Tariff 2 : Day Rate",
                    Description = "Chargeable from 10pm until 7am and on Sundays and Bank Holidays except where tariff 3 applies.",
                    InitialCharge = 4.50,
                    FirstMileCharge = 6.60,
                    AdditionalMileCharge = 4.20
                },

                new Tariff
                {
                    Id = 3,
                    Type = TariffType.Tariff_3,
                    Name = "Tariff 3 : Day Rate",
                    Description = "Chargeable on Christmas Day, Boxing Day, New Years Day. Plus from 6pm on Christmas Eve and New Years Eve.",
                    InitialCharge = 6.00,
                    FirstMileCharge = 8.80,
                    AdditionalMileCharge = 5.60
                }
            };

            return list;
        }

        // Local POIs
        public List<LocalPOI> GetLocalPOIsForInsert()
        {
            var list = new List<LocalPOI>
            {
                 new LocalPOI
                 {
                    Id = 1,
                    Address = "Gill Station",
                    Postcode = "SP8 4PZ",
                    Type = LocalPOIType.Train_Station,
                    Longitude = -2.2766784,
                    Latitude = 51.0385367
                 },
                new LocalPOI
                    {
                        Id = 2,
                        Address = "ASDA Gill",
                        Postcode = "SP8 4QA",
                        Type = LocalPOIType.Supermarket,
                        Longitude = -2.27445,
                        Latitude = 51.03560
                    },
                new LocalPOI
                    {
                        Id = 3,
                        Address = "Waitrose Gill",
                        Postcode = "SP8 4UA",
                        Type = LocalPOIType.Supermarket,
                        Longitude = -2.27646,
                        Latitude = 51.03618
                    },
                new LocalPOI
                    {
                        Id = 4,
                        Address = "Iceland Gill",
                        Postcode = "SP8 4PY",
                        Type = LocalPOIType.Supermarket,
                        Longitude = -2.28274,
                        Latitude = 51.04566
                    },
                new LocalPOI
                    {
                        Id = 5,
                        Address = "LIDL Gill",
                        Postcode = "SP8 4QJ",
                        Type = LocalPOIType.Supermarket,
                        Longitude = -2.28274,
                        Latitude = 51.04566
                    },
                new LocalPOI
                    {
                        Id = 6,
                        Address = "ALDI Gill",
                        Postcode = "SP8 5FB",
                        Type = LocalPOIType.Supermarket,
                        Longitude = -2.28274,
                        Latitude = 51.04566
                    },
                new LocalPOI
                    {
                        Id = 7,
                        Address = "Red Lion Pub",
                        Postcode = "SP8 4AA",
                        Type = LocalPOIType.Pub,
                        Longitude = -2.28274,
                        Latitude = 51.04566
                    },
                new LocalPOI
                    {
                        Id = 8,
                        Address = "Phoenix Pub",
                        Postcode = "SP8 4AY",
                        Type = LocalPOIType.Pub,
                        Longitude = -2.28274,
                        Latitude = 51.04566
                    },
                new LocalPOI
                    {
                        Id = 9,
                        Address = "Rockys Bar",
                        Postcode = "SP8 4DZ",
                        Type = LocalPOIType.Pub,
                        Longitude = -2.28274,
                        Latitude = 51.04566
                    },
                new LocalPOI
                    {
                        Id = 10,
                        Address = "Barn Surgery",
                        Postcode = "SP8 4XS",
                        Type = LocalPOIType.Doctors,
                        Longitude = -0,
                        Latitude = 0
                    },
                new LocalPOI
                    {
                        Id = 11,
                        Address = "Peacemarsh Surgery",
                        Postcode = "SP8 4FA",
                        Type = LocalPOIType.Doctors,
                        Longitude = -2.28274,
                        Latitude = 51.04566
                    }
            };

            return list;
        }
    }
}

