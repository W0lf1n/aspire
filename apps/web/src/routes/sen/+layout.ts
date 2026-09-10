/**
 * A dream's screens have an id in the path, so there is nothing for the
 * build to prerender: the shell's 200.html carries them and the app takes
 * over in the browser, as it does for every screen.
 */
export const prerender = false;
