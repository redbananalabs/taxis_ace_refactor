using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;


namespace TaxiDispatch.Data
{
    public class ScheduleData
    {
        public List<Booking> GetAppointments()
        {
            List<Booking> bookings = new List<Booking>();

            for (int i = 0; i < 30; i++)
            {
                var scope = GetRandomScope();

                PaymentType ptype = PaymentType.Account;
                PaymentStatus pstatus = PaymentStatus.NA;
                ConfirmationStatus confirmed = ConfirmationStatus.Confirmed;
                var recur = string.Empty;

                if(scope != BookingScope.SchoolRun)
                {
                    ptype = GetRandomPaymentType();
                    pstatus = GetRandomPaymentStatus();
                    confirmed = GetRandomStatus(pstatus);
                }

                var start = GetRandomTimeToday();

                if(scope == BookingScope.SchoolRun)
                    recur = GetRandomRecurrenceRule(start.Minute);

                bookings.Add(new Booking
                {
                    Id = i + 1,
                    PickupAddress = $"{RandomString(10 + i)}",
                    PickupPostCode = RandomString(4) + " " + RandomString(3),
                    Details = $"{RandomString(100)}",
                    
                    PickupDateTime = start,
                    EndTime = GetEndTime(start),
                    BookedByName = RandomString(i+5), 
                    Scope = scope,
                    ConfirmationStatus = confirmed,
                    PaymentType = ptype,
                    PaymentStatus = pstatus,
                    RecurrenceRule = recur
                });
            }

            return bookings;
        }

        //public List<ResourceData> GetResourceData()
        //{
        //    List<ResourceData> resourceData = new List<ResourceData>();
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 1,
        //        Subject = "Workflow Analysis",
        //        PickupDateTime = new DateTime(2020, 2, 12, 9, 30, 0),
        //        EndTime = new DateTime(2020, 2, 12, 12, 0, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 2,
        //        Subject = "Requirement planning",
        //        PickupDateTime = new DateTime(2020, 3, 5, 10, 30, 0),
        //        EndTime = new DateTime(2020, 3, 5, 12, 45, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 3,
        //        Subject = "Quality Analysis",
        //        PickupDateTime = new DateTime(2020, 1, 14, 10, 0, 0),
        //        EndTime = new DateTime(2020, 1, 14, 12, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 4,
        //        Subject = "Resource planning",
        //        PickupDateTime = new DateTime(2020, 1, 16, 11, 0, 0),
        //        EndTime = new DateTime(2020, 1, 16, 13, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 5,
        //        Subject = "Timeline estimation",
        //        PickupDateTime = new DateTime(2020, 2, 7, 9, 0, 0),
        //        EndTime = new DateTime(2020, 2, 7, 11, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 6,
        //        Subject = "Developers Meeting",
        //        PickupDateTime = new DateTime(2020, 2, 11, 10, 0, 0),
        //        EndTime = new DateTime(2020, 2, 11, 12, 45, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 7,
        //        Subject = "Project Review",
        //        PickupDateTime = new DateTime(2020, 2, 4, 11, 15, 0),
        //        EndTime = new DateTime(2020, 2, 4, 13, 0, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 8,
        //        Subject = "Manual testing",
        //        PickupDateTime = new DateTime(2020, 3, 8, 9, 15, 0),
        //        EndTime = new DateTime(2020, 3, 8, 11, 45, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 9,
        //        Subject = "Project Preview",
        //        PickupDateTime = new DateTime(2020, 2, 2, 9, 30, 0),
        //        EndTime = new DateTime(2020, 2, 2, 12, 45, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 10,
        //        Subject = "Cross-browser testing",
        //        PickupDateTime = new DateTime(2020, 2, 17, 13, 45, 0),
        //        EndTime = new DateTime(2020, 2, 17, 16, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 11,
        //        Subject = "Bug Automation",
        //        PickupDateTime = new DateTime(2020, 2, 26, 10, 0, 0),
        //        EndTime = new DateTime(2020, 2, 26, 12, 15, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 12,
        //        Subject = "Functionality testing",
        //        PickupDateTime = new DateTime(2020, 2, 25, 9, 0, 0),
        //        EndTime = new DateTime(2020, 2, 25, 11, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 13,
        //        Subject = "Resolution-based testing",
        //        PickupDateTime = new DateTime(2020, 2, 19, 9, 30, 0),
        //        EndTime = new DateTime(2020, 2, 19, 11, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 14,
        //        Subject = "Test report Validation",
        //        PickupDateTime = new DateTime(2020, 3, 15, 9, 0, 0),
        //        EndTime = new DateTime(2020, 3, 15, 11, 0, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 15,
        //        Subject = "Test case correction",
        //        PickupDateTime = new DateTime(2020, 3, 18, 9, 45, 0),
        //        EndTime = new DateTime(2020, 3, 18, 11, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 16,
        //        Subject = "Run test cases",
        //        PickupDateTime = new DateTime(2020, 1, 19, 10, 30, 0),
        //        EndTime = new DateTime(2020, 1, 19, 13, 0, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 17,
        //        Subject = "Quality Analysis",
        //        PickupDateTime = new DateTime(2020, 2, 12, 9, 0, 0),
        //        EndTime = new DateTime(2020, 2, 12, 11, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 18,
        //        Subject = "Debugging",
        //        PickupDateTime = new DateTime(2020, 2, 13, 9, 0, 0),
        //        EndTime = new DateTime(2020, 2, 13, 11, 15, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 19,
        //        Subject = "Exception handling",
        //        PickupDateTime = new DateTime(2020, 2, 16, 10, 10, 0),
        //        EndTime = new DateTime(2020, 2, 16, 13, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 2,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 20,
        //        Subject = "Decoding",
        //        PickupDateTime = new DateTime(2020, 2, 28, 10, 30, 0),
        //        EndTime = new DateTime(2020, 2, 28, 12, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 2
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 21,
        //        Subject = "Requirement planning",
        //        PickupDateTime = new DateTime(2020, 2, 18, 9, 30, 0),
        //        EndTime = new DateTime(2020, 2, 18, 11, 45, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 1
        //    });
        //    resourceData.Add(new ResourceData
        //    {
        //        Id = 19,
        //        Subject = "Exception handling",
        //        PickupDateTime = new DateTime(2020, 2, 18, 10, 10, 0),
        //        EndTime = new DateTime(2020, 2, 18, 13, 30, 0),
        //        IsAllDay = false,
        //        ProjectId = 1,
        //        CategoryId = 2
        //    });
        //    return resourceData;
        //}

        public List<Booking> GetRecurrenceData()
        {
            List<Booking> recurrenceData = new List<Booking>();
            recurrenceData.Add(new Booking
            {
                Id = 1,
                Subject = "Project demo meeting with Andrew",
                Location = "Office",
                PickupDateTime = new DateTime(2020, 1, 8, 9, 0, 0),
                EndTime = new DateTime(2020, 1, 8, 10, 30, 0),
                RecurrenceRule = "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO;COUNT=10",
                Description = "Project demo meeting with Andrew regarding timeline"
            });
            recurrenceData.Add(new Booking
            {
                Id = 2,
                Subject = "Scrum Meeting",
                Location = "Office",
                PickupDateTime = new DateTime(2020, 1, 6, 12, 0, 0),
                EndTime = new DateTime(2020, 1, 6, 13, 0, 0),
                RecurrenceRule = "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;INTERVAL=1",
                Description = "Weekly work status"
            });
            recurrenceData.Add(new Booking
            {
                Id = 3,
                Subject = "Meeting with Core team",
                Location = "Office",
                PickupDateTime = new DateTime(2020, 1, 10, 9, 0, 0),
                EndTime = new DateTime(2020, 1, 10, 10, 30, 0),
                RecurrenceRule = "FREQ=WEEKLY;INTERVAL=1;BYDAY=FR",
                Description = "Future plans and posibilities"
            });
            recurrenceData.Add(new Booking
            {
                Id = 4,
                Subject = "Customer meeting – John Mackenzie",
                Location = "Office",
                PickupDateTime = new DateTime(2020, 2, 14, 10, 30, 0),
                EndTime = new DateTime(2020, 2, 14, 11, 30, 0),
                RecurrenceRule = "FREQ=MONTHLY;BYMONTHDAY=20;INTERVAL=1;COUNT=5",
                Description = "Regarding DataSource issue"
            });
            return recurrenceData;
        }

        private Random random = new Random();

        private DateTime GetRandomTimeToday()
        {
            var dd = DateTime.Now.Day;
            var mm = DateTime.Now.Month;
            var yy = 2023;
            
            var hh = random.Next(9, 18);
            var min = random.Next(0, 30);

            return new DateTime(yy, mm, dd, hh, min,0);
        }

        private DateTime GetEndTime(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute + 25, 0);
        }

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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

        private PaymentType GetRandomPaymentType() // 2 options
        {
            var rand = random.Next(1, 100);
            var res = rand < 50 ? 2 : 3;

            return (PaymentType)res;
        }

        private BookingScope GetRandomScope() // 2 options 
        {
            var rand = random.Next(1, 100);
            var res = rand < 50 ? 1 : 2;

            return (BookingScope)res;
        }

        private string GetRandomRecurrenceRule(int minutes)
        {
            var r1 = "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO;COUNT=10";
            var r2 = "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;INTERVAL=1";
            var r3 = "FREQ=WEEKLY;INTERVAL=1;BYDAY=FR";
            var r4 = "FREQ=MONTHLY;BYMONTHDAY=20;INTERVAL=1;COUNT=5";
            var r5 = "";

            if(minutes < 10)
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

        public class ResourceData : Booking
        {
            public int ProjectId { get; set; }
            public int CategoryId { get; set; }
        }
    }
}

