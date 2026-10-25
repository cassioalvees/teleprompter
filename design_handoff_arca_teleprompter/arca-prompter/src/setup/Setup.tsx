// @ts-nocheck — lógica portada de Instalador.dc.html. No app (Setup.exe) as etapas são reais e vêm do
// processo principal (electron/setup.ts); aberto num navegador, mantém a simulação do protótipo
// para revisão visual.
import React from 'react';
import { renderTemplate } from './SetupTemplate';
import './setup.css';
import './setup-hover.css';

const arca = window.arca;
const real = !!arca;

export default class Setup extends React.Component<{ startMode: 'install' | 'uninstall' }, any> {
  state = {
    mode: this.props.startMode ?? 'install', step: 0, accepted: false,
    path: 'C:\\Program Files\\ARCA Teleprompter', scope: 'all',
    comp: { core: true, ndi: false, web: true, voice: false, fonts: true, samples: true },
    desk: true, startMenu: true, assoc: true, launch: true, notes: false,
    pct: 0, task: '', file: '', log: [], cancelAsk: false, cancelled: false,
    keep: { scripts: true, prefs: true, profiles: true, logs: false }, running: false, reason: '', feedback: '',
    info: null, disk: null, error: ''
  };
  // [id, nome, descrição, MB, obrigatório, indisponível]
  COMP = [
    ['core', 'Aplicativo ARCA Teleprompter', 'Editor, janela do talento, saídas para monitores e controle remoto.', 0, true],
    ['web', 'Monitor web', 'Servidor local para acompanhar o prompter pelo navegador na mesma rede (libera a porta 8420 no firewall).', 0],
    ['fonts', 'Fontes de leitura', 'Atkinson Hyperlegible, Source Sans 3, Source Serif 4 e IBM Plex Mono (já incluídas no aplicativo).', 0, true],
    ['samples', 'Roteiros de exemplo', 'Roteiros para testar marcadores, pausas e apresentadores, em Documentos › ARCA Teleprompter.', 1],
    ['ndi', 'Saída NDI', 'Envio do prompter ao vMix e outros softwares pela rede. Disponível numa próxima versão.', 0, false, true],
    ['voice', 'Acompanhamento por voz offline', 'Modelo de fala em português executado no computador. Disponível numa próxima versão.', 0, false, true]
  ];
  INSTALL_STEPS = ['Boas-vindas', 'Licença', 'Local', 'Componentes', 'Instalação', 'Concluído'];
  UN_STEPS = ['Confirmar', 'Motivo', 'Remoção', 'Concluído'];

  componentDidMount() {
    if (!real) { this.tick = setInterval(() => this.advance(), 120); return; }
    arca.on('setup:progress', p => this.onProgress(p));
    arca.invoke('setup:info').then(info => {
      const st: any = { info, running: !!info.appRunning };
      if (info.mode === 'install') { st.scope = info.isAdmin ? 'all' : 'me'; st.path = info.isAdmin ? info.paths.all : info.paths.me; }
      if (info.resume) Object.assign(st, info.resume, { step: 3 });
      this.setState(st, () => { this.refreshDisk(); if (info.resume) this.startInstall(); });
    });
  }
  componentWillUnmount() { clearInterval(this.tick); }

  refreshDisk() { if (real && this.state.mode === 'install') arca.invoke('setup:disk', this.state.path).then(disk => this.setState({ disk })); }
  onProgress(p) {
    this.setState(x => ({
      pct: p.pct ?? x.pct, task: p.task ?? x.task, file: p.file ?? x.file,
      log: p.log ? [...x.log, { t: p.log, mark: p.warn ? '!' : '✓', c: p.warn ? '#b26b00' : '#2f8f5b' }] : x.log,
      ...(p.done ? { step: x.step + 1, pct: 100, task: 'Concluído', file: '' } : {}),
      ...(p.error ? { error: p.error, cancelled: true, task: 'A instalação falhou', file: p.error } : {})
    }));
  }

  // ——— simulação (somente no navegador) ———
  advance() {
    const s = this.state, busy = s.mode === 'install' ? s.step === 4 : s.step === 2;
    if (!busy || s.cancelAsk || s.cancelled) return;
    if (!s.tasks || !s.tasks.length) return;
    const tasks = s.tasks, n = tasks.length, p = Math.min(100, s.pct + 2 + Math.random() * 3), idx = Math.min(n - 1, Math.floor(p / (100 / n)));
    const log = [...s.log]; for (let k = s.ti; k < idx; k++) log.push({ t: tasks[k][0], mark: '✓', c: '#2f8f5b' });
    if (p >= 100) { log.push({ t: tasks[n - 1][0], mark: '✓', c: '#2f8f5b' }); this.setState({ pct: 100, ti: n, task: 'Concluído', file: '', log, step: s.step + 1, tasks: null }); return; }
    const t = tasks[idx]; this.setState({ pct: p, ti: idx, log, task: t[0], file: t[1] });
  }
  run(tasks, step) { this.setState({ step, tasks, ti: 0, pct: 0, log: [], task: tasks[0][0], file: tasks[0][1], cancelled: false }); }

  size() { const i = this.state.info; return i ? i.sizeMB + (this.state.comp.samples ? 1 : 0) : 186; }
  setMode(mode) { if (real) return; this.setState({ mode, tasks: null, ti: 0, step: 0, pct: 0, log: [], cancelAsk: false, cancelled: false }); }

  async startInstall() {
    const s = this.state;
    if (!real) return this.run([['Preparando a instalação', 'Verificando espaço e permissões'], ['Copiando o aplicativo', s.path], ['Criando atalhos', 'Menu Iniciar · Área de trabalho'], ['Registrando o desinstalador', 'Aplicativos instalados']], 4);
    const opts = { path: s.path.trim(), scope: s.scope, comp: s.comp, desk: s.desk, startMenu: s.startMenu, assoc: s.assoc };
    // "Todos os usuários" grava em Arquivos de Programas: reabre o Setup como administrador (UAC).
    if (s.scope === 'all' && !s.info.isAdmin) {
      const r = await arca.invoke('setup:elevate', opts);
      if (!r.ok) this.setState({ error: r.error || 'Permissão de administrador negada.' });
      return;
    }
    this.setState({ step: 4, pct: 0, log: [], task: 'Preparando a instalação', file: '', cancelled: false, error: '' });
    arca.invoke('setup:install', opts);
  }
  startUninstall() {
    if (!real) return this.run([['Fechando o ARCA Teleprompter', 'Encerrando processos'], ['Removendo arquivos do programa', 'Pasta de instalação'], ['Removendo registro do desinstalador', 'Aplicativos instalados']], 2);
    this.setState({ step: 2, pct: 0, log: [], task: 'Removendo', file: '', error: '' });
    arca.invoke('setup:uninstall', { keep: this.state.keep, reason: this.state.reason, feedback: this.state.feedback });
  }

  render() { return renderTemplate(this.renderVals()); }

  renderVals() {
    const s = this.state, inst = s.mode === 'install', names = inst ? this.INSTALL_STEPS : this.UN_STEPS, step = s.step, info = s.info;
    const busy = inst ? step === 4 : step === 2, done = inst ? step === 5 : step === 3;
    const pathOk = /^[a-zA-Z]:\\[^<>:"|?*]*$/.test(s.path.trim());
    const ck = (on, fn, label) => ({ on, onToggle: fn, label });
    const sz = this.size();
    const ver = info ? info.version : '1.0.0', product = 'ARCA Teleprompter';
    const failed = !!s.error && busy;
    const C = {
      install: [
        ['Boas-vindas', 'Instalar o ARCA Teleprompter', 'Este assistente instala o teleprompter no seu computador. Leva cerca de um minuto.'],
        ['Licença', 'Contrato de licença', 'Leia os termos antes de continuar.'],
        ['Local', 'Onde instalar', 'Escolha a pasta e para quem o aplicativo ficará disponível.'],
        ['Componentes', 'Escolha os componentes', s.error || 'Você pode adicionar ou remover componentes depois, instalando de novo.'],
        ['Instalação', failed ? 'A instalação falhou' : s.cancelled ? 'Instalação cancelada' : 'Instalando', failed ? 'As alterações foram desfeitas. ' + s.error : s.cancelled ? 'Nenhuma alteração foi mantida no computador.' : 'Não desligue o computador durante a instalação.'],
        ['Concluído', 'Tudo pronto', 'O ARCA Teleprompter foi instalado.']
      ],
      uninstall: [
        ['Desinstalar', 'Remover o ARCA Teleprompter', 'Escolha o que manter. Os arquivos mantidos serão usados se você instalar de novo.'],
        ['Motivo', 'Por que está removendo?', 'Opcional. Fica registrado só neste computador.'],
        ['Remoção', s.error ? 'Remoção incompleta' : 'Removendo', s.error || 'Isso leva alguns segundos.'],
        ['Concluído', 'Desinstalação concluída', 'O ARCA Teleprompter foi removido deste computador.']
      ]
    }[s.mode][step];
    const nextLabel = inst ? ['Avançar', 'Avançar', 'Avançar', 'Instalar', failed || s.cancelled ? 'Fechar' : 'Instalando…', s.launch ? 'Abrir o app' : 'Concluir'][step] : ['Desinstalar', 'Remover', s.error ? 'Fechar' : 'Removendo…', 'Fechar'][step];
    const nextDisabled = (busy && !(failed || s.cancelled || (!inst && s.error))) || (inst && step === 1 && !s.accepted) || (inst && step === 2 && !pathOk);
    const close = () => real ? arca.send('setup:quit') : this.setMode(s.mode);
    const free = s.disk ? s.disk.freeGB : 214.6;
    return {
      windowed: real,
      minimize: () => real && arca.send('setup:minimize'),
      versionLine: `Versão ${ver} · 64 bits`,
      modes: [['install', 'Instalação'], ['uninstall', 'Desinstalação']].map(([v, l]) => ({ label: l, onClick: () => this.setMode(v), bg: s.mode === v ? '#ffffff' : 'transparent', shadow: s.mode === v ? '0 1px 2px rgba(26,26,24,.12)' : 'none', color: s.mode === v ? '#1a1a18' : '#5a5a55' })),
      screenLabel: (inst ? 'Instalação · ' : 'Desinstalação · ') + names[step],
      sideCaption: inst ? 'Assistente de instalação' : 'Desinstalador',
      winTitle: inst ? 'Instalação do ARCA Teleprompter' : 'Desinstalar ARCA Teleprompter',
      steps: names.map((n, i) => { const past = i < step || (done && i === step), cur = i === step && !done; return { label: n, mark: past ? '✓' : String(i + 1), dotBg: past ? '#ff5a4a' : 'transparent', dotFg: past ? '#1a0705' : cur ? '#ececef' : '#6e6e76', dotRing: past ? 'none' : cur ? 'inset 0 0 0 1.5px #ff5a4a' : 'inset 0 0 0 1px #3a3a40', color: cur ? '#ffffff' : past ? '#c8c8ce' : '#6e6e76', weight: cur ? 600 : 500 }; }),
      eyebrow: C[0], title: C[1], lead: C[2],
      isWelcome: inst && step === 0, isLicense: inst && step === 1, isLocation: inst && step === 2, isComponents: inst && step === 3, isProgress: busy, isDone: done,
      isUnConfirm: !inst && step === 0, isUnFeedback: !inst && step === 1,
      welcomeRows: [['Versão', ver + ' (64 bits)'], ['Requisitos', 'Windows 10 21H2 ou superior · 4 GB de RAM'], ['Espaço', '≈ ' + sz + ' MB'], ['Editor', 'ARCA']].map(([k, v], i) => ({ k, v, line: i ? '#ebebeb' : 'transparent' })),
      accepted: s.accepted, toggleAccept: () => this.setState(x => ({ accepted: !x.accepted })),
      path: s.path, onPath: e => this.setState({ path: e.target.value }, () => this.refreshDisk()), pathErr: !pathOk,
      browse: async () => {
        if (!real) return this.setState({ path: 'D:\\Aplicativos\\ARCA Teleprompter' });
        const p = await arca.invoke('setup:browse', s.path); if (p) this.setState({ path: p }, () => this.refreshDisk());
      },
      scopes: [['all', 'Todos os usuários'], ['me', 'Somente para mim']].map(([v, l]) => ({ label: l, onClick: () => this.setState({ scope: v, path: real ? info.paths[v] : (v === 'me' ? 'C:\\Users\\Operador\\AppData\\Local\\Programs\\ARCA Teleprompter' : 'C:\\Program Files\\ARCA Teleprompter') }, () => this.refreshDisk()), bg: s.scope === v ? '#ffffff' : 'transparent', shadow: s.scope === v ? '0 1px 2px rgba(26,26,24,.1)' : 'none', color: s.scope === v ? '#1a1a18' : '#5a5a55' })),
      diskStats: [['Necessário', sz + ' MB'], ['Disponível', free == null ? '—' : free.toFixed(1) + ' GB'], ['Unidade', (s.path.trim()[0] || 'C').toUpperCase() + ':']].map(([k, v]) => ({ k, v })),
      components: this.COMP.map(([id, name, desc, mb, req, na], i) => ({ name: req ? name + ' (obrigatório)' : na ? name + ' (em breve)' : name, desc, size: na ? '—' : id === 'core' ? sz + ' MB' : mb ? mb + ' MB' : 'incluído', on: na ? false : s.comp[id], disabled: !!req || !!na, cursor: req || na ? 'default' : 'pointer', bg: req || na ? '#fafaf8' : '#ffffff', line: i ? '#ebebeb' : 'transparent', onToggle: () => !req && !na && this.setState(x => ({ comp: { ...x.comp, [id]: !x.comp[id] } })) })),
      extras: [ck(s.startMenu, () => this.setState(x => ({ startMenu: !x.startMenu })), 'Criar atalho no Menu Iniciar'), ck(s.desk, () => this.setState(x => ({ desk: !x.desk })), 'Criar atalho na área de trabalho'), ck(s.assoc, () => this.setState(x => ({ assoc: !x.assoc })), 'Abrir arquivos .tps com o ARCA Teleprompter')],
      progTask: s.cancelled && !s.error ? 'Revertendo alterações' : s.task, progPct: Math.round(s.pct) + '%', progFile: s.file, log: s.log,
      doneHead: inst ? `${product} ${ver} instalado` : `${product} removido`,
      doneSub: inst ? s.path : (Object.values(s.keep).some(Boolean) ? 'Seus dados mantidos ficam em %APPDATA%\\ARCA Teleprompter' : 'Todos os dados foram apagados'),
      doneChecks: inst ? [ck(s.launch, () => this.setState(x => ({ launch: !x.launch })), 'Abrir o ARCA Teleprompter agora'), ck(s.notes, () => this.setState(x => ({ notes: !x.notes })), 'Ver as novidades desta versão')] : [],
      unTitle: `${product} ${info && info.installed ? info.installed.version : ver}`,
      unPathLine: info && info.installed ? `${info.installed.path} · ${info.installed.sizeMB} MB` : 'C:\\Program Files\\ARCA Teleprompter · 318 MB',
      keepOpts: [['scripts', 'Manter roteiros e versões', 'Abas abertas, histórico e salvamento automático.'], ['prefs', 'Manter preferências', 'Atalhos, telas, monitor web e integrações.'], ['profiles', 'Manter perfis de apresentador', 'Fonte, margem, cores e velocidade de cada perfil.'], ['logs', 'Manter registros e cache', 'Arquivos temporários e cache do navegador interno.']].map(([id, name, desc], i) => ({ name, desc, size: info && info.dataSizes ? info.dataSizes[id] : ['12 MB', '<1 MB', '<1 MB', '84 MB'][i], on: s.keep[id], line: i ? '#ebebeb' : 'transparent', onToggle: () => this.setState(x => ({ keep: { ...x.keep, [id]: !x.keep[id] } })) })),
      appRunning: s.running, closeApp: async () => { if (!real) return this.setState({ running: false }); const r = await arca.invoke('setup:closeApp'); this.setState({ running: !r }); },
      reasons: ['Vou reinstalar', 'Instalei uma versão nova', 'Não uso mais', 'Problemas de desempenho', 'Falta um recurso', 'Outro'].map(l => ({ label: l, onClick: () => this.setState({ reason: s.reason === l ? '' : l }), bg: s.reason === l ? '#1a1a18' : '#ffffff', fg: s.reason === l ? '#ffffff' : '#1a1a18', bd: s.reason === l ? '#1a1a18' : '#e2e2de' })),
      feedback: s.feedback, onFeedback: e => this.setState({ feedback: e.target.value }),
      showBack: !busy && !done && step > 0, showCancel: !done && !(busy && (s.cancelled || s.error)) && !(!inst && step === 2),
      back: () => this.setState({ step: Math.max(0, step - 1), error: '' }),
      next: () => {
        if (nextDisabled) return;
        if (busy && (s.cancelled || s.error)) return close();
        if (inst) {
          if (step === 3) return this.startInstall();
          if (step === 5) { if (real) return arca.send('setup:finish', { launch: s.launch, notes: s.notes }); return this.setMode('install'); }
          return this.setState({ step: step + 1 });
        }
        if (step === 1) return this.startUninstall();
        if (step === 3) return close();
        this.setState({ step: step + 1 });
      },
      nextLabel, nextDisabled, nextBg: nextDisabled ? '#e2e2de' : '#d42b1e', nextFg: nextDisabled ? '#8a8a84' : '#ffffff', nextCursor: nextDisabled ? 'default' : 'pointer',
      askCancel: () => { if (done || (busy && (s.cancelled || s.error))) return close(); this.setState({ cancelAsk: true }); }, confirmCancel: s.cancelAsk,
      cancelTitle: inst ? 'Cancelar a instalação?' : 'Cancelar a desinstalação?',
      cancelText: busy ? 'Os arquivos já copiados serão removidos e nenhuma alteração será mantida.' : (inst ? 'O ARCA Teleprompter não será instalado.' : 'O ARCA Teleprompter continuará instalado.'),
      cancelBtn: inst ? 'Cancelar instalação' : 'Cancelar',
      keepGoing: () => this.setState({ cancelAsk: false }),
      doCancel: () => {
        if (!busy) return close();
        this.setState({ cancelAsk: false, cancelled: true, tasks: null, task: 'Revertendo alterações', file: '' });
        if (real) arca.invoke('setup:cancel'); else setTimeout(() => this.setMode(s.mode), 1400);
      }
    };
  }
}
