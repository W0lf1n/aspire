/**
 * The build emits a static shell and the app takes over in the browser. There
 * is nothing useful to render on a server: the board is fetched from the API
 * with the device's token, and the token lives on the device.
 */
export const ssr = false;
export const prerender = true;
