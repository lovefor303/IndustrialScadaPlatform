using System.Net;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Scada.Controls.Svg;

public sealed record SvgPreviewDocument
{
    public SvgPreviewDocument(string fileName, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        FileName = fileName;
        Title = title;
    }

    public string FileName { get; }

    public string Title { get; }
}

/// <summary>
/// Creates a local-only index for generated SVG review files. It emits no scripts and never resolves network resources.
/// </summary>
public sealed class SvgDocumentWriter
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "The writer remains an instance dependency so preview formatting can evolve without changing callers.")]
    public string CreateHtml(string title, IReadOnlyCollection<SvgPreviewDocument> documents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(documents);

        if (documents.Any(document => !IsLocalSvgFileName(document.FileName)))
        {
            throw new ArgumentException(
                "Preview document file names must be local .svg file names without path separators.",
                nameof(documents));
        }

        var ordered = documents
            .OrderBy(document => document.FileName, StringComparer.Ordinal)
            .ThenBy(document => document.Title, StringComparer.Ordinal)
            .ToArray();
        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\"><title>")
            .Append(Encode(title))
            .Append("</title><style>")
            .Append("body{margin:0;background:#202428;color:#D4D9DC;font-family:'Segoe UI',sans-serif;}main{padding:24px;}h1{font-size:20px;font-weight:600;margin:0 0 20px;}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:16px;}.card{border:1px solid #343B40;padding:12px;background:#292F33;}.card h2{font-size:14px;font-weight:600;margin:0 0 10px;}.card object{width:100%;height:180px;border:0;background:#202428;}</style></head><body><main><h1>")
            .Append(Encode(title))
            .Append("</h1><section class=\"grid\">");

        foreach (var document in ordered)
        {
            html.Append("<article class=\"card\"><h2>")
                .Append(Encode(document.Title))
                .Append("</h2><object type=\"image/svg+xml\" data=\"")
                .Append(Encode(document.FileName))
                .Append("\"></object></article>");
        }

        html.Append("</section></main></body></html>");
        return html.ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static bool IsLocalSvgFileName(string fileName) =>
        fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
        && fileName.IndexOfAny(['/', '\\', ':']) < 0
        && string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal);
}
