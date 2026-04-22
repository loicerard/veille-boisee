using System.Text;
using MediatR;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Common;
using VeilleBoisee.Domain.Enums;

namespace VeilleBoisee.Application.Reports.Queries;

public sealed record ExportReportsCsvQuery(
    IReadOnlyList<string> InseeCodes,
    ReportStatus? StatusFilter,
    DateTimeOffset? From,
    DateTimeOffset? To
) : IRequest<Result<CsvExportDto, ExportReportsCsvError>>;

public enum ExportReportsCsvError
{
    NoData
}

public sealed record CsvExportDto(byte[] Content, string FileName);

internal sealed class ExportReportsCsvHandler
    : IRequestHandler<ExportReportsCsvQuery, Result<CsvExportDto, ExportReportsCsvError>>
{
    private readonly IReportRepository _repository;

    public ExportReportsCsvHandler(IReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CsvExportDto, ExportReportsCsvError>> Handle(
        ExportReportsCsvQuery request,
        CancellationToken cancellationToken)
    {
        var reports = await _repository.GetAllByInseeCodes(
            request.InseeCodes,
            request.StatusFilter,
            request.From,
            request.To,
            cancellationToken);

        if (reports.Count == 0)
            return ExportReportsCsvError.NoData;

        var csv = new StringBuilder();
        csv.AppendLine("Id,CommuneName,CommuneInsee,Status,SubmittedAt,IsInForest,IsInNatura2000Zone,ParcelleSection,ParcelleNumero");

        foreach (var r in reports)
        {
            csv.Append(r.Id).Append(',');
            csv.Append(EscapeCsv(r.CommuneName)).Append(',');
            csv.Append(EscapeCsv(r.CommuneInsee)).Append(',');
            csv.Append(r.Status).Append(',');
            csv.Append(r.SubmittedAt.ToString("O")).Append(',');
            csv.Append(r.IsInForest?.ToString() ?? string.Empty).Append(',');
            csv.Append(r.IsInNatura2000Zone?.ToString() ?? string.Empty).Append(',');
            csv.Append(EscapeCsv(r.ParcelleSection ?? string.Empty)).Append(',');
            csv.AppendLine(EscapeCsv(r.ParcelleNumero ?? string.Empty));
        }

        var content = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"signalements-{DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";

        return new CsvExportDto(content, fileName);
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
