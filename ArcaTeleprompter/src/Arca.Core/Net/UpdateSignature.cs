using System.Security.Cryptography;
using System.Text;

namespace Arca.Core.Net;

/// <summary>
/// Assinatura do latest.json da atualização automática (ECDSA P-256, assinatura IEEE P1363 em base64url). Quem
/// publica assina "versão|arquivo|sha256|tamanho" com a chave privada de atualizações (fora do repositório); o app
/// confere com a chave pública embutida. Sem isso, quem conseguisse publicar no GitHub (conta invadida) poderia
/// mandar um instalador falso com o SHA-256 dele, e o app aceitaria.
/// </summary>
public static class UpdateSignature
{
    /// <summary>Chave pública de atualizações (SPKI, base64). A privada fica em server/licencas/.dev.vars.</summary>
    public const string PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEMHYfU4Gy+SG9ivjhPOluWTMidH0w/5c5okgyBzbShz36cNr/HuALcrlRIa6A/KoJD30piiMuodDbFxVRhcqU6A==";

    /// <summary>Texto assinado: os campos que decidem o que é instalado.</summary>
    public static string Payload(string version, string file, string sha256, long size) => $"{version}|{file}|{sha256.ToUpperInvariant()}|{size}";

    public static bool Verify(string payload, string? signature, string publicKey = PublicKey)
    {
        if (string.IsNullOrWhiteSpace(signature)) return false;
        try
        {
            var sig = Convert.FromBase64String(signature.Replace('-', '+').Replace('_', '/').PadRight(signature.Length + (4 - signature.Length % 4) % 4, '='));
            using var ec = ECDsa.Create();
            ec.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
            return ec.VerifyData(Encoding.UTF8.GetBytes(payload), sig, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch { return false; }
    }
}
