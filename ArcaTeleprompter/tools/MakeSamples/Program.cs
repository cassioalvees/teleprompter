// Gera os roteiros de exemplo (.tps) instalados com o componente "Roteiros de exemplo".
using Arca.Core.Engine;
using Arca.Core.Model;

var dir = args.Length > 0 ? args[0] : "samples";
Directory.CreateDirectory(dir);
var s = new PrompterSettings();
void Save(string name, ScriptDocument d) => File.WriteAllText(Path.Combine(dir, name), TpsFile.Serialize(d, s));

Save("Jornal da Noite.tps", SampleScripts.JornalDaNoite());

var e = new ScriptDocument();
e.Blocks.Add(ScriptBlock.Mark("Abertura"));
e.Blocks.Add(ScriptBlock.Paragraph("Olá! Hoje a gente conversa sobre como montar um roteiro no ARCA Teleprompter.", 1));
e.Blocks.Add(ScriptBlock.Paragraph("E eu vou mostrando cada recurso enquanto você lê.", 2));
e.Blocks.Add(ScriptBlock.Mark("Apresentadores"));
e.Blocks.Add(ScriptBlock.Paragraph("Cada fala pode ter um apresentador. Use Alt+1, Alt+2 ou o seletor na barra de ferramentas.", 1));
e.Blocks.Add(ScriptBlock.Paragraph("A cor e o nome aparecem no prompter, acima de cada fala.", 2));
e.Blocks.Add(ScriptBlock.Pause("Entra arte · 10 s"));
e.Blocks.Add(ScriptBlock.Mark("Encerramento"));
e.Blocks.Add(ScriptBlock.Paragraph("Obrigado e até a próxima.", 1));
Save("Entrevista com dois apresentadores.tps", e);

var t = new ScriptDocument();
t.Blocks.Add(ScriptBlock.Mark("Bloco 1 · Velocidade"));
t.Blocks.Add(ScriptBlock.Paragraph("Use as setas para cima e para baixo para ajustar a velocidade. Segure Shift para passos maiores."));
t.Blocks.Add(ScriptBlock.Paragraph("As teclas de 1 a 9 escolhem velocidades predefinidas."));
t.Blocks.Add(ScriptBlock.Mark("Bloco 2 · Marcadores"));
t.Blocks.Add(ScriptBlock.Paragraph("Page Up e Page Down saltam entre os marcadores, como num clicker de apresentação."));
t.Blocks.Add(ScriptBlock.Pause("Pausa para respirar"));
t.Blocks.Add(ScriptBlock.Paragraph("Ao chegar numa pausa, a rolagem para. Aperte espaço para continuar."));
t.Blocks.Add(ScriptBlock.Mark("Bloco 3 · Voz"));
t.Blocks.Add(ScriptBlock.Paragraph("Ligue o acompanhamento por voz na aba Voz e leia este texto em voz alta. A rolagem segue a sua leitura e para quando você improvisa."));
Save("Treino de leitura.tps", t);
Console.WriteLine("ok: " + Directory.GetFiles(dir).Length + " roteiros");
