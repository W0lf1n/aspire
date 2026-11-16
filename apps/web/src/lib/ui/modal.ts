/**
 * `use:modal={open}` — a `<dialog>` shown as a modal while `open` is true.
 *
 * `showModal` on an open dialog throws, and `close` on a closed one fires a
 * second `close` event, so it asks the element what it already is.
 */
export function modal(dialog: HTMLDialogElement, open: boolean) {
	const sync = (open: boolean) => {
		if (open && !dialog.open) dialog.showModal();
		else if (!open && dialog.open) dialog.close();
	};
	sync(open);
	return { update: sync };
}
