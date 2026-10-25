# ARCA Teleprompter 2.6

Teleprompter profissional para Windows (C# / .NET 8 / WPF) com monitor web/PWA para tablets,
saída NDI, acompanhamento por voz offline, integração MOS com sistemas de redação e instalador próprio.

> Substitui o protótipo em React/Electron (`design_handoff_arca_teleprompter/arca-prompter`), mantido só como referência.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `src/Arca.Core` | DLL de domínio: modelo do roteiro, `.tps`, layout, motor de rolagem, alinhador de voz, persistência. Sem dependência de UI. |
| `src/Arca.Import` | DLL de importação: DOCX (OpenXML), PDF (PdfPig), HTML, TXT, RTF, TPS. |
| `src/Arca.Audio` | DLL de áudio: captura WASAPI/ASIO (NAudio), reamostragem e reconhecimento de fala (Vosk). |
| `src/Arca.Ndi` | DLL NDI: P/Invoke para o runtime NDI 6 (`Processing.NDI.Lib.x64.dll`). |
| `src/Arca.Server` | DLL do servidor web: Kestrel + WebSocket, HTTPS autoassinado, PWA embutido (`wwwroot`). |
| `src/Arca.Mos` | DLL MOS 2.8 (subconjunto de running order) via TCP 10540/10541. |
| `src/ArcaTeleprompter` | Aplicativo WPF (editor, prompter, telas, talento, inspetor, serviços). |
| `src/ArcaSetup` | Instalador / desinstalador (WPF, arquivo único). |
| `tests/Arca.Core.Tests` | Testes automatizados (xUnit). |
| `tools/` | Ferramentas de verificação: `NdiTest`, `NdiProbe` (`--medir N`: regularidade dos quadros), `VoiceTest`, `ServerTest`, `MakeSamples`. |
| `docs/` | Documentação de engenharia ([índice](docs/README.md)) e manual do operador. |
| `CHANGELOG.md` | [Histórico de versões](CHANGELOG.md) — o que mudou em cada versão. |

## Compilar

Requisitos: Windows 10 1809+ x64, .NET SDK 8.0.

```bash
dotnet build ArcaTeleprompter.sln
```

```bash
dotnet test tests/Arca.Core.Tests
```

Gerar o instalador (testes → publicação → pacote → `dist\ARCA Teleprompter Setup 2.0.0.exe`):

```bash
powershell -ExecutionPolicy Bypass -File build.ps1
```

Opções: `-SkipTests`, `-NoVoiceModel` (instalador sem o modelo de voz; o app baixa quando precisar).

## Dependências (todas públicas)

CommunityToolkit.Mvvm (MIT) · NAudio (MIT) · Vosk (Apache-2.0) · DocumentFormat.OpenXml (MIT) ·
PdfPig (Apache-2.0) · QRCoder (MIT) · ASP.NET Core (MIT) · xUnit (Apache-2.0) ·
fontes Atkinson Hyperlegible, Source Sans 3, Source Serif 4, IBM Plex Mono, Raleway, Sora (OFL) ·
NDI® runtime (licença NDI, instalado à parte pelo usuário).
