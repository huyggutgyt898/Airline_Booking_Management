using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace TEO_Airline_Project.Services;

public sealed class FlightPricingExportRow
{
    public string FlightNumber { get; init; } = "";

    public string DepartureAirportCode { get; init; } = "";

    public string ArrivalAirportCode { get; init; } = "";

    public DateTime DepartureTime { get; init; }

    public string AircraftModel { get; init; } = "";

    public string SeatClass { get; init; } = "";

    public string PassengerType { get; init; } = "";

    public decimal BasePrice { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal ServiceFee { get; init; }
}

public static class FlightPricingExcelExporter
{
    private const string SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Create(IReadOnlyList<FlightPricingExportRow> rows)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", WriteContentTypes);
            WriteEntry(archive, "_rels/.rels", WritePackageRelationships);
            WriteEntry(archive, "xl/workbook.xml", WriteWorkbook);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WriteWorkbookRelationships);
            WriteEntry(archive, "xl/styles.xml", WriteStyles);
            WriteEntry(archive, "xl/worksheets/sheet1.xml", writer => WriteWorksheet(writer, rows));
        }

        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string path, Action<XmlWriter> write)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            CloseOutput = false
        });
        write(writer);
    }

    private static void WriteContentTypes(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Types", ContentTypesNamespace);
        WriteEmptyElement(writer, ContentTypesNamespace, "Default", ("Extension", "rels"), ("ContentType", "application/vnd.openxmlformats-package.relationships+xml"));
        WriteEmptyElement(writer, ContentTypesNamespace, "Default", ("Extension", "xml"), ("ContentType", "application/xml"));
        WriteEmptyElement(writer, ContentTypesNamespace, "Override", ("PartName", "/xl/workbook.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"));
        WriteEmptyElement(writer, ContentTypesNamespace, "Override", ("PartName", "/xl/worksheets/sheet1.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"));
        WriteEmptyElement(writer, ContentTypesNamespace, "Override", ("PartName", "/xl/styles.xml"), ("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"));
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WritePackageRelationships(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", PackageRelationshipsNamespace);
        WriteEmptyElement(writer, PackageRelationshipsNamespace, "Relationship",
            ("Id", "rId1"),
            ("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
            ("Target", "xl/workbook.xml"));
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbook(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("workbook", SpreadsheetNamespace);
        writer.WriteAttributeString("xmlns", "r", null, RelationshipsNamespace);
        writer.WriteStartElement("sheets", SpreadsheetNamespace);
        writer.WriteStartElement("sheet", SpreadsheetNamespace);
        writer.WriteAttributeString("name", "Bảng giá");
        writer.WriteAttributeString("sheetId", "1");
        writer.WriteAttributeString("r", "id", RelationshipsNamespace, "rId1");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorkbookRelationships(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Relationships", PackageRelationshipsNamespace);
        WriteEmptyElement(writer, PackageRelationshipsNamespace, "Relationship",
            ("Id", "rId1"),
            ("Type", $"{RelationshipsNamespace}/worksheet"),
            ("Target", "worksheets/sheet1.xml"));
        WriteEmptyElement(writer, PackageRelationshipsNamespace, "Relationship",
            ("Id", "rId2"),
            ("Type", $"{RelationshipsNamespace}/styles"),
            ("Target", "styles.xml"));
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteStyles(XmlWriter writer)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("styleSheet", SpreadsheetNamespace);
        writer.WriteStartElement("numFmts", SpreadsheetNamespace);
        writer.WriteAttributeString("count", "1");
        WriteEmptyElement(writer, "numFmt", ("numFmtId", "164"), ("formatCode", "#,##0.00"));
        writer.WriteEndElement();
        writer.WriteStartElement("fonts", SpreadsheetNamespace);
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("font", SpreadsheetNamespace);
        WriteEmptyElement(writer, "sz", ("val", "11"));
        WriteEmptyElement(writer, "name", ("val", "Calibri"));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("fills", SpreadsheetNamespace);
        writer.WriteAttributeString("count", "2");
        writer.WriteStartElement("fill", SpreadsheetNamespace);
        WriteEmptyElement(writer, "patternFill", ("patternType", "none"));
        writer.WriteEndElement();
        writer.WriteStartElement("fill", SpreadsheetNamespace);
        WriteEmptyElement(writer, "patternFill", ("patternType", "gray125"));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("borders", SpreadsheetNamespace);
        writer.WriteAttributeString("count", "1");
        writer.WriteStartElement("border", SpreadsheetNamespace);
        WriteEmptyElement(writer, "left");
        WriteEmptyElement(writer, "right");
        WriteEmptyElement(writer, "top");
        WriteEmptyElement(writer, "bottom");
        WriteEmptyElement(writer, "diagonal");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("cellStyleXfs", SpreadsheetNamespace);
        writer.WriteAttributeString("count", "1");
        WriteEmptyElement(writer, "xf", ("numFmtId", "0"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"));
        writer.WriteEndElement();
        writer.WriteStartElement("cellXfs", SpreadsheetNamespace);
        writer.WriteAttributeString("count", "2");
        WriteEmptyElement(writer, "xf", ("numFmtId", "0"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"), ("xfId", "0"));
        WriteEmptyElement(writer, "xf", ("numFmtId", "164"), ("fontId", "0"), ("fillId", "0"), ("borderId", "0"), ("xfId", "0"), ("applyNumberFormat", "1"));
        writer.WriteEndElement();
        writer.WriteStartElement("cellStyles", SpreadsheetNamespace);
        writer.WriteAttributeString("count", "1");
        WriteEmptyElement(writer, "cellStyle", ("name", "Normal"), ("xfId", "0"), ("builtinId", "0"));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteWorksheet(
        XmlWriter writer,
        IReadOnlyList<FlightPricingExportRow> rows)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", SpreadsheetNamespace);
        writer.WriteStartElement("cols", SpreadsheetNamespace);
        WriteColumn(writer, 1, 1, 14);
        WriteColumn(writer, 2, 3, 12);
        WriteColumn(writer, 4, 4, 20);
        WriteColumn(writer, 5, 5, 22);
        WriteColumn(writer, 6, 7, 16);
        WriteColumn(writer, 8, 11, 18);
        writer.WriteEndElement();
        writer.WriteStartElement("sheetData", SpreadsheetNamespace);

        var headers = new[]
        {
            "Chuyến bay", "Sân bay đi", "Sân bay đến", "Khởi hành", "Model máy bay",
            "Hạng ghế", "Loại khách", "Giá cơ bản", "Thuế", "Phí dịch vụ", "Tổng giá"
        };
        writer.WriteStartElement("row", SpreadsheetNamespace);
        writer.WriteAttributeString("r", "1");
        for (var index = 0; index < headers.Length; index++)
        {
            WriteTextCell(writer, $"{ColumnName(index + 1)}1", headers[index]);
        }
        writer.WriteEndElement();

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];
            writer.WriteStartElement("row", SpreadsheetNamespace);
            writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
            WriteTextCell(writer, $"A{rowNumber}", row.FlightNumber);
            WriteTextCell(writer, $"B{rowNumber}", row.DepartureAirportCode);
            WriteTextCell(writer, $"C{rowNumber}", row.ArrivalAirportCode);
            WriteTextCell(writer, $"D{rowNumber}", row.DepartureTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));
            WriteTextCell(writer, $"E{rowNumber}", row.AircraftModel);
            WriteTextCell(writer, $"F{rowNumber}", SeatClassLabel(row.SeatClass));
            WriteTextCell(writer, $"G{rowNumber}", PassengerTypeLabel(row.PassengerType));
            WriteNumberCell(writer, $"H{rowNumber}", row.BasePrice);
            WriteNumberCell(writer, $"I{rowNumber}", row.TaxAmount);
            WriteNumberCell(writer, $"J{rowNumber}", row.ServiceFee);
            WriteNumberCell(writer, $"K{rowNumber}", row.BasePrice + row.TaxAmount + row.ServiceFee);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void WriteColumn(XmlWriter writer, int min, int max, int width)
    {
        writer.WriteStartElement("col", SpreadsheetNamespace);
        writer.WriteAttributeString("min", min.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("max", max.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("width", width.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("customWidth", "1");
        writer.WriteEndElement();
    }

    private static void WriteTextCell(XmlWriter writer, string reference, string value)
    {
        writer.WriteStartElement("c", SpreadsheetNamespace);
        writer.WriteAttributeString("r", reference);
        writer.WriteAttributeString("t", "inlineStr");
        writer.WriteStartElement("is", SpreadsheetNamespace);
        writer.WriteStartElement("t", SpreadsheetNamespace);
        writer.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
        writer.WriteString(value);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteNumberCell(XmlWriter writer, string reference, decimal value)
    {
        writer.WriteStartElement("c", SpreadsheetNamespace);
        writer.WriteAttributeString("r", reference);
        writer.WriteAttributeString("s", "1");
        writer.WriteElementString("v", SpreadsheetNamespace, value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }

    private static void WriteEmptyElement(
        XmlWriter writer,
        string name,
        params (string Name, string Value)[] attributes)
    {
        WriteEmptyElement(writer, SpreadsheetNamespace, name, attributes);
    }

    private static void WriteEmptyElement(
        XmlWriter writer,
        string xmlNamespace,
        string name,
        params (string Name, string Value)[] attributes)
    {
        writer.WriteStartElement(name, xmlNamespace);
        foreach (var attribute in attributes)
        {
            writer.WriteAttributeString(attribute.Name, attribute.Value);
        }
        writer.WriteEndElement();
    }

    private static string ColumnName(int column)
    {
        var name = "";
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return name;
    }

    private static string SeatClassLabel(string value) => value switch
    {
        "Economy" => "Phổ thông",
        "Business" => "Thương gia",
        _ => value
    };

    private static string PassengerTypeLabel(string value) => value switch
    {
        "Adult" => "Người lớn",
        "Child" => "Trẻ em",
        "Infant" => "Em bé",
        _ => value
    };
}
