#nullable disable
using TaxiDispatch.Data.Models;
using TaxiDispatch.Domain;
using TaxiDispatch.Interfaces;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static TaxiDispatch.DTOs.Booking.AvailableHours;

namespace TaxiDispatch.DTOs.Booking
{
    public class AccountInvoiceDto
    {
        public AccountInvoiceDto()
        {
            Items = new();
        }

        public string CompanyNo { get; set; }
        public string VatNo { get; set; }


        public int InvoiceNumber { get; set; }
        public string OrderNo { get; set; }
        public string Reference { get; set; }
        public string AccNo { get; set; }

        public decimal Net { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }

        public bool Paid { get; set; }

        public DateTime InvoiceDate { get; set; }
        
        public Address CustomerAddress { get; set; }

        public List<JourneyItem> Items { get; set; }

        public string Comments { get; set; }

        public class Address
        {
            public string ContactName { get; set; }
            public string BusinessName { get; set; }
            public string Address1 { get; set; }
            public string Address2 { get; set; }
            public string Address3 { get; set; }
            public string Address4 { get; set; }
            public string Postcode { get; set; }
        }
    }
}
