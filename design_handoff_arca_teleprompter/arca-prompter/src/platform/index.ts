import { createDesktopPlatform } from './desktop';
import { createWebPlatform } from './web';
import type { Platform } from './types';

export * from './types';

export const platform: Platform = window.arca ? createDesktopPlatform(window.arca) : createWebPlatform();
export const isDesktop = platform.kind === 'desktop';
