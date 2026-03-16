using TaxiDispatch.Data.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace TaxiDispatch.Services
{
    public class GoogleCalendarService
    {
        private const string CalendarId = "icl6kunhcrtabncidlnscuipms@group.calendar.google.com";

        public GoogleCalendarService()
        {

        }

        public async Task<Events> GetGoogleEvents(DateTime start, DateTime? end = null)
        {
            UserCredential credential = await GetCredentials();

            var service = new CalendarService(
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Ace"
                });

            if (!end.HasValue)
            { end = DateTime.Now.AddYears(2); }


            var request = service.Events.List(CalendarId);
            request.TimeMin = start;
            request.TimeMax = end;

            var res = await request.ExecuteAsync();

            return res;
        }

        public async Task<UserCredential> GetCredentials()
        {
            var scopes = new string[] {
                CalendarService.Scope.Calendar,
                CalendarService.Scope.CalendarEvents
            };

            UserCredential credential;
            using (var stream = new FileStream("auth.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes, "user", CancellationToken.None, new FileDataStore("Calender"));
            }

            return credential;
        }

        public List<Booking> ConvertToBookings(Events events)
        {
            List<Booking> eventDatas = new List<Booking>();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (Event eventItem in events.Items)
                {
                    if (eventItem.Start == null && eventItem.Status == "cancelled")
                    {
                        continue;
                    }

                    DateTime start;
                    DateTime end;

                    if (string.IsNullOrEmpty(eventItem.Start.Date))
                    {
                        start = (DateTime)eventItem.Start.DateTime;
                        end = (DateTime)eventItem.End.DateTime;
                    }
                    else
                    {
                        start = Convert.ToDateTime(eventItem.Start.Date);
                        end = Convert.ToDateTime(eventItem.End.Date);
                    }

                    Booking eventData = new Booking()
                    {
                        //GoogleEventId = eventItem.Id,
                        //GoogleSubject = eventItem.Summary,
                        //PickupDateTime = start,
                        //GoogleLocation = eventItem.Location,
                        //GoogleDescription = eventItem.Description,
                        IsAllDay = !string.IsNullOrEmpty(eventItem.Start.Date)

                    };
                    eventDatas.Add(eventData);
                }
            }
            return eventDatas;

        }
    }
}

