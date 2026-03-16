using TaxiDispatch.DTOs.Booking;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TaxiDispatch.PDF
{
    public class AccountCreditNoteDocument : IDocument
    {
        public static Image LogoImage { get; } = Image.FromFile("wwwroot/img/logo_horizontal.png");

        public AccountInvoiceDto Model { get; }
        private int _creditNoteId;
        private DateTime _date;
        private string _reason;

        public AccountCreditNoteDocument(AccountInvoiceDto model, string reason, int creditNoteId, DateTime date)
        {
            Model = model;
            _reason = reason;
            _creditNoteId = creditNoteId;
            _date = date;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
               .Page(page =>
               {
                   page.Size(PageSizes.A4.Landscape());

                   page.Margin(30);
                   page.MarginBottom(0);

                   page.Header().Element(ComposeHeader);
                   page.Content().Element(ComposeContent);

                   page.Footer().Height(60).AlignCenter().Element(ComposeFooter);
               });
        }

        void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignCenter().DefaultTextStyle(new TextStyle().Bold()).Text(text =>
                    {
                        text.Line($"VAT NUMBER: {Model.VatNo}     -     COMPANY NO: {Model.CompanyNo}").SemiBold().FontSize(8);

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
                    col.Item().Text($"CREDIT NOTE")
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
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().DefaultTextStyle(new TextStyle().Bold()).Text(txt =>
                        {
                            txt.Line(Model.CustomerAddress.BusinessName);
                            txt.Line(Model.CustomerAddress.Address1);
                            txt.Line(Model.CustomerAddress.Address2);
                            txt.Line(Model.CustomerAddress.Address3);
                            txt.Line(Model.CustomerAddress.Address4);
                            txt.Line(Model.CustomerAddress.Postcode);
                        });
                    });
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().AlignRight().Width(300).Table(tbl =>
                        {
                            tbl.ColumnsDefinition(col1 =>
                            {
                                col1.RelativeColumn();
                                col1.RelativeColumn();
                            });

                            tbl.Cell().Row(1).Column(1).AlignRight().PaddingRight(10).Text("CREDIT NOTE #:").SemiBold();
                            tbl.Cell().Row(1).Column(2).AlignLeft().Text(_creditNoteId.ToString());
                            tbl.Cell().Row(2).Column(1).AlignRight().PaddingRight(10).Text("DATE:").SemiBold();
                            tbl.Cell().Row(2).Column(2).Text(_date.ToString("dd/MM/yy"));
                            tbl.Cell().Row(3).Column(1).AlignRight().PaddingRight(10).Text("ORIGINAL INVOICE #:").SemiBold();
                            tbl.Cell().Row(3).Column(2).Text(Model.InvoiceNumber.ToString());
                            tbl.Cell().Row(4).Column(1).AlignRight().PaddingRight(10).Text("INVOICE DATE:").SemiBold();
                            tbl.Cell().Row(4).Column(2).Text(Model.InvoiceDate.ToString("dd/MM/yyyy"));
                            tbl.Cell().Row(5).Column(1).AlignRight().PaddingRight(10).Text("ACCOUNT NO:").SemiBold();
                            tbl.Cell().Row(5).Column(2).Text(Model.AccNo);
                        });
                    });
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().PaddingTop(-20).Column(col =>
                    {
                        col.Item().AlignCenter().Text(text =>
                        {
                            text.Line("CREDIT SUMMARY").Bold().FontSize(20);
                            text.Span("Reason: ".PadRight(10)).Bold().FontSize(14);
                            text.Span(_reason).FontSize(14);
                        });

                        col.Item().LineHorizontal(1);
                        col.Spacing(5);

                        col.Item().PaddingRight(40).AlignRight().Text(text =>
                        {
                            text.Span("NET: ".PadRight(10)).Bold().FontSize(24);
                            text.Span(Model.Net.ToString("C")).FontSize(24);
                        });

                        col.Item().PaddingRight(40).AlignRight().Text(text =>
                        {
                            text.Span("VAT: ".PadRight(10)).Bold().FontSize(24);
                            text.Span(Model.Vat.ToString("C")).FontSize(24);
                        });
                        col.Item().PaddingRight(40).AlignRight().Text(text =>
                        {
                            text.Span("TOTAL: ".PadRight(10)).Bold().FontSize(24);
                            text.Span(Model.Total.ToString("C")).FontSize(24);
                        });
                        col.Item().LineHorizontal(1);
                    });
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignCenter().Text("Thank you for using Ace Taxis.").Bold();
                        //col.Item().AlignCenter().Text(text =>
                        //{
                        //    text.Span("ACC NO: ".PadRight(5)).ExtraBold().Style(new TextStyle().Color("E34234").Bold());
                        //    text.Span("73770281").ExtraBold();
                        //    text.Span("|".PadLeft(5).PadRight(10));
                        //    text.Span("SORT CODE: ".PadRight(5)).ExtraBold().Style(new TextStyle().Color("E34234").Bold());
                        //    text.Span("20-99-40").ExtraBold();
                        //});
                    });
                });

                column.Item().PageBreak();

                column.Spacing(20);

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
                    columns.ConstantColumn(40);
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
                    header.Cell().Text("COA").Style(headerStyle);
                    header.Cell().Text("PICKUP").Style(headerStyle);
                    header.Cell().Text("DESTINATION").Style(headerStyle);
                    header.Cell().AlignRight().Text("PARKING").Style(headerStyle);
                    header.Cell().AlignRight().Text("WAITING TIME").Style(headerStyle);
                    header.Cell().AlignRight().Text("WAITING").Style(headerStyle);
                    header.Cell().AlignRight().Text("JOURNEY").Style(headerStyle);
                    header.Cell().AlignRight().Text("TOTAL").Style(headerStyle);

                    header.Cell().ColumnSpan(11).PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var item in Model.Items)
                {
                    //var index = Model.Items.IndexOf(item) + 1;
                    var coa = item.COA ? "COA" : "";
                    table.Cell().Element(CellStyle).Text($"{item.JobNo}").Style(rowStyle);
                    table.Cell().Element(CellStyle).Text(item.Date.ToString("dd/MM/yy HH:mm")).Style(rowStyle);
                    table.Cell().Element(CellStyle).PaddingRight(5).Text(item.Passenger).Style(rowStyle);
                    table.Cell().Element(CellStyle).Text($"{coa}").Style(rowStyle);
                    table.Cell().ShowEntire().Element(CellStyle).PaddingRight(5).Text(item.Pickup).Style(rowStyle);
                    table.Cell().ShowEntire().Element(CellStyle).PaddingRight(5).Text(item.Destination).Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Parking:C}").Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.WaitingTime}").Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Waiting:C}").Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Journey:C}").Style(rowStyle);
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Parking + item.Journey + item.Waiting:C}").Style(rowStyle);

                    static IContainer CellStyle(IContainer container) =>
                        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            });
        }
    }
}

