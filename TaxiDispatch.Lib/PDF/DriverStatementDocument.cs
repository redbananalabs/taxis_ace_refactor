using TaxiDispatch.DTOs.Booking;
using TaxiDispatch.DTOs.MessageTemplates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TaxiDispatch.PDF
{
    public class DriverStatementDocument : IDocument
    {
        public static Image LogoImage { get; } = Image.FromFile("wwwroot/img/logo_horizontal.png");

        public DriverInvoiceStatementDto Model { get; }
        private readonly dynamic _profile;

        public DriverStatementDocument(DriverInvoiceStatementDto model,dynamic profile)
        {
            Model = model;
            _profile = profile;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
               .Page(page =>
               {
                   page.Size(PageSizes.A4.Landscape());

                   page.Margin(20);
                   page.MarginBottom(0);

                   page.Header().Element(ComposeHeader);
                   page.Content().Element(ComposeContent);

                   page.Footer().Height(55).AlignCenter().Element(ComposeFooter);
               });
        }

        void ComposeFooter(IContainer container) 
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().PaddingLeft(5).PaddingRight(5).PaddingBottom(5).LineHorizontal(1);
                    col.Item().AlignCenter().DefaultTextStyle(new TextStyle().Bold()).Text(text =>
                    {
                        text.Line($"VAT NUMBER: 325 1273 31     -     COMPANY NO: 08920947").SemiBold().FontSize(8);

                        text.AlignCenter();
                        text.Span("Page  ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" / ").FontSize(8);
                        text.TotalPages().FontSize(8);
                        text.Line("");
                    });
                    
                });
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
                        txt.Line("www.acetaxisdorset.co.uk").SemiBold();
                        txt.Line("accounts@acetaxisdorset.co.uk").SemiBold();
                        txt.Line("01747 82 11 11");
                    });
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"DRIVER STATEMENT")
                        .FontSize(35).Bold();
                    col.Item().PaddingLeft(45).Text($"Statement Period: {Model.StartDate:dd MMM yyyy} - {Model.EndDate:dd MMM yyyy}");
                });
            });
        }

        void ComposeContent(IContainer container) 
        {
            container.PaddingVertical(0).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().PaddingLeft(10).DefaultTextStyle(new TextStyle()).Text(txt =>
                        {
                            txt.Line($"({_profile.UserId}) - {_profile.FullName}");
                            txt.Line($"{_profile.RegNo} {_profile.VehicleMake}, {_profile.VehicleModel} ");
                            txt.Line(_profile.VehicleColour + " " + _profile.VehicleType.ToString());
                        });
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Width(200).Table(tbl =>
                        {
                            tbl.ColumnsDefinition(col1 =>
                            {
                                col1.RelativeColumn();
                                col1.RelativeColumn();
                            });

                            tbl.Cell().Row(1).Column(1).AlignRight().PaddingRight(10).Text("STATEMENT #:").SemiBold();
                            tbl.Cell().Row(1).Column(2).AlignLeft().Text(Model.StatementId);
                            tbl.Cell().Row(2).Column(1).AlignRight().PaddingRight(10).Text("DATE:").SemiBold();
                            tbl.Cell().Row(2).Column(2).Text(Model.DateCreated.ToString("dd/MM/yy"));
                        });

                    });
                });

                column.Item().PaddingLeft(5).PaddingRight(5).LineHorizontal(1);
                column.Item().PaddingLeft(5).PaddingRight(5).AlignCenter().Text("STATEMENT SUMMARY").Bold().FontSize(14);
                column.Item().PaddingLeft(5).PaddingRight(5).LineHorizontal(1);


                column.Item().Row(row =>
                    row.RelativeItem().AlignCenter().Column(col =>
                    {
                        col.Item().Width(400).PaddingLeft(50).Table(tbl =>
                        {
                            tbl.ColumnsDefinition(col1 =>
                            {
                                col1.RelativeColumn();
                                col1.RelativeColumn();
                            });

                            tbl.Cell().Row(1).Column(1).AlignRight().PaddingRight(10).Text("Completed Jobs:").Bold().LineHeight(2);
                            tbl.Cell().Row(1).Column(2).AlignLeft().Text(Model.TotalJobCount.ToString()).LineHeight(2);

                            tbl.Cell().Row(2).Column(1).AlignRight().PaddingRight(10).PaddingBottom(5).Text("Earnings Account:").Bold();
                            tbl.Cell().Row(2).Column(2).Text($"£{Model.EarningsAccount:F2}");

                            tbl.Cell().Row(3).Column(1).AlignRight().PaddingBottom(5).PaddingRight(10).Text("Earnings Cash:").Bold();
                            tbl.Cell().Row(3).Column(2).Text($"£{Model.EarningsCash:F2}");

                            tbl.Cell().Row(4).Column(1).AlignRight().PaddingRight(10).PaddingBottom(5).Text("Earnings Card:").Bold();
                            tbl.Cell().Row(4).Column(2).Text($"£{Model.EarningsCard:F2}");

                            tbl.Cell().Row(5).Column(1).AlignRight().PaddingRight(10).Text("Earnings Rank:").Bold();
                            tbl.Cell().Row(5).Column(2).Text($"£{Model.EarningsRank:F2}");

                        });
                    }));

                column.Item().Row(row =>
                {
                    row.RelativeItem().PaddingTop(7).Column(col =>
                    {
                        col.Item().AlignCenter().PaddingBottom(1).Text("TOTAL EARNINGS").Bold().ExtraBold();
                        col.Item().AlignCenter().Text($"£{Model.TotalEarned:N2}").FontSize(12);

                       // col.Item().AlignCenter().PaddingTop(10).PaddingBottom(1).Text("COMMISSION").Bold().ExtraBold();
                       // col.Item().AlignCenter().Text($"£{Model.CommissionDue:N2}").FontSize(12);

                        col.Item().AlignCenter().PaddingBottom(2).PaddingTop(15).Text("PAYMENT DUE/OWED").Bold().ExtraBold().FontColor("E34234");
                        col.Item().AlignCenter().Text($"£{Model.PaymentDue:N2}").FontSize(14);
                    });
                });

                //column.Item().PaddingTop(10).Row(row =>
                //{
                //    row.RelativeItem().Column(col =>
                //    {
                //        col.Item().PaddingTop(2).LineHorizontal(1, Unit.Mil);
                //        col.Item().AlignCenter().PaddingBottom(2).Text("ACE TAXIS (DORSET) LTD.").Bold().FontSize(8);
                //        col.Item().AlignCenter().Text(text =>
                //        {
                //            text.Span("ACC NO: ".PadRight(5)).ExtraBold().Style(new TextStyle().Color("E34234").Bold()).FontSize(8);
                //            text.Span("73770281").ExtraBold().FontSize(8);
                //            text.Span("|".PadLeft(5).PadRight(10)).FontSize(8);
                //            text.Span("SORT CODE: ".PadRight(5)).ExtraBold().Style(new TextStyle().Color("E34234").Bold()).FontSize(8);
                //            text.Span("20-99-40").ExtraBold().FontSize(8);
                //        });
                //        col.Item().PaddingTop(2).LineHorizontal(1, Unit.Mil);
                //    });
                //});

                column.Item().PageBreak();

                column.Spacing(4);

                column.Item().Element(ComposeTable);

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
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(80);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(60);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(50);
                });

                table.Header(header =>
                {
                    header.Cell().Text("JOB #").Style(headerStyle);
                    header.Cell().Text("DATE").Style(headerStyle);
                    header.Cell().Text("PASSENGER").Style(headerStyle);
                    header.Cell().Text("PICKUP").Style(headerStyle);
                    header.Cell().Text("DESTINATION").Style(headerStyle);
                    header.Cell().AlignRight().Text("PARKING").Style(headerStyle);
                    header.Cell().AlignRight().Text("WAITING TIME").Style(headerStyle);
                    header.Cell().AlignRight().Text("WAITING").Style(headerStyle);
                    header.Cell().AlignRight().Text("JOURNEY").Style(headerStyle);
                    header.Cell().AlignRight().Text("TOTAL").Style(headerStyle);

                    header.Cell().ColumnSpan(10).PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var item in Model.Jobs)
                {
                    //var index = Model.Items.IndexOf(item) + 1;

                    table.Cell().Element(CellStyle).Text($"{item.BookingId}").Style(rowStyle);
                    table.Cell().Element(CellStyle).Text(item.Date.ToString("dd/MM/yy HH:mm")).Style(rowStyle);
                    table.Cell().Element(CellStyle).PaddingRight(5).Text(item.Passenger).Style(rowStyle);
                    table.Cell().ShowEntire().Element(CellStyle).PaddingRight(5).Text(item.Pickup).Style(rowStyle);
                    table.Cell().ShowEntire().Element(CellStyle).PaddingRight(5).Text(item.Destination).Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.ParkingCharge:C}").Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.WaitingTime}").Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.WaitingPriceDriver:C}").Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Price:C}").Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.ParkingCharge + item.Price + item.WaitingPriceDriver:C}").Style(rowStyle);

                    static IContainer CellStyle(IContainer container) =>
                        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            });
        }
    }
}

