using PrintMaestro.Core.Printing;

namespace PrintMaestro.Core.Tests;

public class DocumentKindResolverTests
{
    [Theory]
    [InlineData("report.docx", DocumentKind.Office)]
    [InlineData("legacy.doc", DocumentKind.Office)]
    [InlineData("sheet.xlsx", DocumentKind.Office)]
    [InlineData("legacy.xls", DocumentKind.Office)]
    [InlineData("slides.pptx", DocumentKind.Office)]
    [InlineData("legacy.ppt", DocumentKind.Office)]
    [InlineData("scan.pdf", DocumentKind.Pdf)]
    [InlineData("note.txt", DocumentKind.Text)]
    public void Resolve_MapsKnownExtensions(string filePath, DocumentKind expected) =>
        Assert.Equal(expected, DocumentKindResolver.Resolve(filePath));
}
