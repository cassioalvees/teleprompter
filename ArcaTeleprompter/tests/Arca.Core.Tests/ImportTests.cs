using Arca.Core.Model;
using Arca.Import;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Arca.Core.Tests;

/// <summary>Importar DOCX (OpenXml + System.IO.Packaging corrigido na 2.10.1): texto, parágrafos e negrito chegam ao roteiro.</summary>
public class ImportTests
{
    [Fact]
    public void DocxComNegrito()
    {
        var path = Path.Combine(Path.GetTempPath(), $"arca-teste-{Guid.NewGuid():N}.docx");
        try
        {
            using (var d = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                var main = d.AddMainDocumentPart();
                main.Document = new W.Document(new W.Body(
                    new W.Paragraph(new W.Run(new W.Text("Boa noite, começa agora o jornal."))),
                    new W.Paragraph(
                        new W.Run(new W.Text("Chuva forte") { Space = SpaceProcessingModeValues.Preserve }) { RunProperties = new W.RunProperties(new W.Bold()) },
                        new W.Run(new W.Text(" no litoral.") { Space = SpaceProcessingModeValues.Preserve }))));
            }
            var doc = ScriptImporter.FromDocx(path);
            var text = doc.Blocks.Where(b => b.Kind == BlockKind.Text && b.WordCount > 0).ToList();
            Assert.Equal(2, text.Count);
            Assert.Equal("Boa noite, começa agora o jornal.", text[0].PlainText);
            Assert.Equal("Chuva forte no litoral.", text[1].PlainText);
            Assert.Contains(text[1].Runs, r => r.Bold && r.Text.Contains("Chuva forte"));
        }
        finally { File.Delete(path); }
    }
}
