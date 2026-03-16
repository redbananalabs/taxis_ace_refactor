using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TaxiDispatch.PDF
{
    public class PaymentReceipt : IDocument
    {
        public static Image LogoImage { get; } = Image.FromFile("wwwroot/img/logo_horizontal.png");

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public string Passenger { get; set; }
        public string Date { get; set; }
        public string BookingId { get; set; }
        public string Detail { get; set; }
        public string Amount { get; set; }

        public string PaymentType { get; set; }

        public PaymentReceipt(string passenger,string date, string bookingid, string detail, string amount, string paymentType)
        {
            Passenger = passenger;
            Date = date;
            BookingId = bookingid;
            Detail = detail;
            Amount = amount;
            PaymentType = paymentType;
        }

        public void Compose(IDocumentContainer container)
        {
            container
               .Page(page =>
               {
                   page.Size(PageSizes.A4.Portrait());

                   page.Margin(30);
                   page.MarginBottom(0);

                   page.Header().Element(ComposeHeader);
                   page.Content().Element(ComposeContent);
               });
        }

        void ComposeHeader(IContainer container)
        {
            var path = Directory.GetCurrentDirectory();
            container.Row(row =>
            {
                row.RelativeItem().AlignLeft().Column(col =>
                {
                    col.Item().Width(200).Image(LogoImage);
                    col.Item().PaddingLeft(5).DefaultTextStyle(new TextStyle().Bold()).Text(txt =>
                    {
                        txt.Line("www.acetaxisdorset.co.uk").LineHeight(1).SemiBold();
                        txt.Line("accounts@acetaxisdorset.co.uk").LineHeight(2).SemiBold();
                        txt.Line("01747 82 11 11");
                    });
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"RECEIPT")
                        .FontSize(35).Bold();
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(0).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().PaddingTop(5).Column(col =>
                    {
                        col.Item().AlignCenter().Text(text =>
                        {
                            text.Line("PAYMENT RECEIPT").Bold().FontSize(20);
                        });

                        col.Item().LineHorizontal(1);
                        col.Spacing(5);
                    });
                });

                column.Item().Row(row =>
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(100).Padding(5).Column(col =>
                        {
                            col.Item().PaddingBottom(5).AlignLeft().Text("Passenger:").Bold();
                            col.Item().PaddingBottom(5).AlignLeft().Text("Journey Date:").Bold();
                            col.Item().PaddingBottom(5).AlignLeft().Text("Payment Type:").Bold();
                            col.Item().PaddingBottom(5).AlignLeft().Text("Booking Ref:").Bold();
                        });
                        row.ConstantItem(150).Padding(5).Column(col =>
                        {
                            col.Item().PaddingBottom(5).AlignLeft().Text(Passenger);
                            col.Item().PaddingBottom(5).AlignLeft().Text(Date);
                            col.Item().PaddingBottom(5).AlignLeft().Text(PaymentType);
                            col.Item().PaddingBottom(5).AlignLeft().Text(BookingId);
                        });
                    });
                    column.Item().LineHorizontal(1);
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().PaddingTop(10).Column(col =>
                    {
                        col.Item().Element(ComposeTable);
                    });
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().PaddingTop(350).Column(col =>
                    {
                        col.Item().AlignCenter().Text("This is a payment receipt for a journey booked with Ace Taxis.").Bold();
                    });
                });

                column.Spacing(20);
            });
        }
        void ComposeTable(IContainer container)
        {
            var headerStyle = TextStyle.Default.SemiBold().FontSize(8);
            var rowStyle = TextStyle.Default.FontSize(8);

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(50);
                    columns.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    header.Cell().Column(1).Text("DETAILS").Style(headerStyle);
                    header.Cell().Column(2).Text("PRICE").Style(headerStyle);
                    header.Cell().ColumnSpan(2).PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                table.Cell().Column(1).Element(CellStyle).Text(Detail).Style(rowStyle);
                table.Cell().Column(2).Element(CellStyle).Text(Amount).Style(rowStyle);

                static IContainer CellStyle(IContainer container) =>
                    container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
            });
        }
    }
}

