# 07 — Casos de uso

Atores: **Operador** (PC), **Apresentador/Talento**, **Sistema de redação** (MOS), **Mixer** (NDI/legenda).

| UC | Nome | Ator | Fluxo principal | Alternativos / exceções |
|---|---|---|---|---|
| UC-01 | Preparar roteiro | Operador | Novo (Ctrl+N) → digitar/colar/importar → `*Título` vira marcador → atribuir apresentadores → salvar | Arquivo corrompido: mensagem e nada é perdido; DOCX/PDF sem texto: aviso |
| UC-02 | Rodar no ar | Operador | Espaço → contagem → rolagem; ↑/↓ ajustam velocidade; Page Up/Down navegam marcadores; pausa automática nas pausas | Travar edição durante o play (preferência); fim do roteiro para sozinho |
| UC-03 | Configurar telas | Operador | Inspetor › Telas → escolher monitor → espelhar H → ligar | Monitor do operador pede confirmação; monitor desconectado: saída fecha e avisa |
| UC-04 | Usar iPad como prompter | Operador, Talento | Ligar Web → ler QR code → (HTTPS) instalar certificado → Adicionar à Tela de Início → modo limpo ou controle | Sem senha correta: tela de login; rede caiu: reconecta sozinho |
| UC-05 | Enviar para mixer | Operador | Inspetor › Telas › NDI → formato → ligar → fonte aparece no mixer | Sem runtime NDI: link para baixar |
| UC-06 | Seguir a voz | Operador, Talento | Inspetor › Voz → dispositivo (WASAPI/ASIO + canal) → Usar microfone → a rolagem segue a fala | Improviso: para e mostra "IMPROVISO"; volta ao texto: ressincroniza |
| UC-07 | Receber espelho | Sistema de redação | NRCS conecta em 10540/10541 → roCreate/roStorySend → espelho aparece → operador aplica | Mensagem desconhecida: ignorada e registrada |
| UC-08 | Corrigir no ar | Talento/Operador | E (editar trecho atual) → corrige → Enter | Escape cancela |
| UC-09 | Recuperar versão | Operador | Arquivo › Histórico de versões → escolher → restaurar (vira nova versão) | — |
| UC-10 | Instalar / desinstalar | TI | Setup → pasta → componentes → instalar; Configurações › Aplicativos → desinstalar (manter dados opcional) | Pasta protegida: pede elevação (UAC); app aberto: "Fechar agora" |
