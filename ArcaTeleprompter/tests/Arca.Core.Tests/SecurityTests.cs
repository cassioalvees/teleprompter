using Arca.Core.Model;
using Arca.Core.Net;

namespace Arca.Core.Tests;

/// <summary>Cor do texto só como cor (sem CSS extra) e assinatura do latest.json da atualização automática.</summary>
public class SecurityTests
{
    [Theory]
    [InlineData("#FFD400", true)]
    [InlineData("#fff", true)]
    [InlineData("rgb(255, 0, 10)", true)]
    [InlineData("rgba(10,20,30,0.5)", true)]
    [InlineData("red", true)]
    [InlineData("red;background:url(https://x.com/a)", false)]
    [InlineData("expression(alert(1))", false)]
    [InlineData("#12345678901", false)]
    [InlineData("", false)]
    public void SafeColor(string color, bool ok) => Assert.Equal(ok, HtmlScript.SafeColor(color));

    [Fact]
    public void ColorWithCssDoesNotReachHtml()
    {
        var html = HtmlScript.RunHtml(new TextRun("Texto") { Color = "red;background:url(https://x.com/a)" });
        Assert.DoesNotContain("style", html);
        Assert.Contains("Texto", html);
    }

    // Vetor de teste gerado pelo mesmo método do tools/assinar-atualizacao.mjs (node, ECDSA P-256, IEEE P1363,
    // base64url), com um par de chaves só de teste.
    private const string TestKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE8psMTkWBxffQLK/283y6T1BPDAwVAm63Cy58SDMqDVQfrOm/bIKiP395VmKH3gpOsRiJf7rp8WVRpE9DAvxkQw==";
    private const string TestSig = "C-2xfYeoZ6w1FizYHmygTCB30XK6mAwYO2wa8eh7-GI-HyXLArpbm2ZOAqRlvG0Ztl92BQBp1fB4229MdJ9Ofg";
    private const string Sha = "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789";

    [Fact]
    public void UpdateSignatureFromPublisherIsAccepted()
        => Assert.True(UpdateSignature.Verify(UpdateSignature.Payload("9.9.9", "ARCA-Teleprompter-Setup-9.9.9.exe", Sha.ToLowerInvariant(), 123456), TestSig, TestKey));

    [Theory]
    [InlineData("9.9.8", "ARCA-Teleprompter-Setup-9.9.9.exe", 123456)]   // outra versão
    [InlineData("9.9.9", "outro.exe", 123456)]                           // outro arquivo
    [InlineData("9.9.9", "ARCA-Teleprompter-Setup-9.9.9.exe", 123457)]   // outro tamanho
    public void ChangedUpdateIsRejected(string version, string file, long size)
        => Assert.False(UpdateSignature.Verify(UpdateSignature.Payload(version, file, Sha, size), TestSig, TestKey));

    [Fact]
    public void MissingOrWrongKeySignatureIsRejected()
    {
        var payload = UpdateSignature.Payload("9.9.9", "ARCA-Teleprompter-Setup-9.9.9.exe", Sha, 123456);
        Assert.False(UpdateSignature.Verify(payload, null, TestKey));
        Assert.False(UpdateSignature.Verify(payload, "", TestKey));
        Assert.False(UpdateSignature.Verify(payload, TestSig));   // chave de verdade do app: assinatura de outra chave não vale
        Assert.False(UpdateSignature.Verify(payload, "lixo!!", TestKey));
    }
}
