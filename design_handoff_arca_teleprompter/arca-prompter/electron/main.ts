// Um único executável, três modos:
//  • instalador   — quando roda a partir do Setup (alvo "portable" do electron-builder) ou com --setup
//  • desinstalador — com --uninstall (é o comando registrado em "Aplicativos instalados")
//  • aplicativo   — no restante dos casos
import { app } from 'electron';
import { startApp } from './app';
import { startSetup } from './setup';

app.setAppUserModelId('com.arca.teleprompter');

const argv = process.argv;
const forced = process.env.ARCA_MODE;
if (forced === 'uninstall' || argv.includes('--uninstall')) startSetup('uninstall');
else if (forced === 'setup' || argv.includes('--setup') || (process.env.PORTABLE_EXECUTABLE_FILE && !argv.includes('--run'))) startSetup('install');
else startApp();
