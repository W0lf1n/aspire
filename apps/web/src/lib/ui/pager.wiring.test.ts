/**
 * The pager's wiring, on a scroll region made of arithmetic.
 *
 * The Browser pane on this machine produces no frames, and both a scroll
 * event and an animation frame are dispatched by the frame loop, so a fence
 * that is checked on scroll cannot be watched there at all (`apps/web
 * /CLAUDE.md`). It can be driven here: a fake element that moves the way a
 * browser moves one, a frame queue the test turns by hand, and the pager on
 * top of them. `pager.test.ts` beside this has the arithmetic itself.
 */

import { describe, expect, it, vi } from 'vitest';
import { pager } from './pager';

/** The listener half of an element, or of the window. */
function listening() {
	const map = new Map<string, Set<(event: never) => void>>();
	return {
		addEventListener(type: string, fn: (event: never) => void) {
			const set = map.get(type) ?? new Set<(event: never) => void>();
			map.set(type, set);
			set.add(fn);
		},
		removeEventListener(type: string, fn: (event: never) => void) {
			map.get(type)?.delete(fn);
		},
		fire(type: string, event: unknown) {
			for (const fn of [...(map.get(type) ?? [])]) fn(event as never);
		}
	};
}

/** A reel of `pages` screens, each `height` tall, and the pager driving it. */
function harness(pages = 5, height = 800) {
	const frames: Array<(now: number) => void> = [];
	const ears = listening();
	const sill = listening();
	const seen: number[] = [];

	const el = {
		scrollTop: 0,
		style: {} as Record<string, string>,
		getBoundingClientRect: () => ({ height }),
		addEventListener: ears.addEventListener,
		removeEventListener: ears.removeEventListener
	};

	const globals = globalThis as unknown as Record<string, unknown>;
	const before = { ...globals };

	globals.matchMedia = () => ({ matches: false });
	globals.ResizeObserver = class {
		observe() {}
		disconnect() {}
	};
	globals.getComputedStyle = () => ({ getPropertyValue: () => '360ms' });
	globals.requestAnimationFrame = (fn: (now: number) => void) => frames.push(fn);
	globals.cancelAnimationFrame = () => frames.splice(0);
	globals.HTMLElement = class {};
	globals.window = sill;

	const drive = pager(el as unknown as HTMLElement, {
		pages: () => pages,
		onPage: (index) => seen.push(index)
	});

	return {
		el,
		drive,
		seen,

		/** What a browser does: move, stop at the ends, then say so. */
		scroll(to: number) {
			el.scrollTop = Math.min(Math.max(to, 0), (pages - 1) * height);
			ears.fire('scroll', {});
		},

		touch(type: 'touchstart' | 'touchend', fingers: number) {
			ears.fire(type, { touches: { length: fingers } });
		},

		wheel(deltaY: number) {
			sill.fire('wheel', { deltaY, deltaX: 0, deltaMode: 0, preventDefault() {} });
		},

		sideways() {
			let prevented = false;
			sill.fire('wheel', {
				deltaY: 10,
				deltaX: 400,
				deltaMode: 0,
				preventDefault: () => {
					prevented = true;
				}
			});
			return prevented;
		},

		key(key: string) {
			sill.fire('keydown', { key, target: null, preventDefault() {} });
		},

		/** Turn the glide over: one frame past its duration is its end. */
		settle() {
			const now = performance.now() + 10_000;
			for (const frame of frames.splice(0)) frame(now);
		},

		rest() {
			drive.destroy();
			for (const key of [
				'matchMedia',
				'ResizeObserver',
				'getComputedStyle',
				'requestAnimationFrame',
				'cancelAnimationFrame',
				'HTMLElement',
				'window'
			]) {
				if (key in before) globals[key] = before[key];
				else delete globals[key];
			}
		}
	};
}

describe('the fence', () => {
	it('holds a swipe to the next dream, however far it carries', () => {
		const reel = harness();
		reel.el.scrollTop = 800;

		reel.touch('touchstart', 1);
		reel.scroll(100_000); // the finger took the whole reel with it

		expect(reel.el.scrollTop).toBe(1600);
		reel.rest();
	});

	it('holds a swipe back the same way', () => {
		const reel = harness();
		reel.el.scrollTop = 2400;

		reel.touch('touchstart', 1);
		reel.scroll(-100_000);

		expect(reel.el.scrollTop).toBe(1600);
		reel.rest();
	});

	it('leaves a gesture alone inside its own page', () => {
		const reel = harness();
		reel.el.scrollTop = 800;

		reel.touch('touchstart', 1);
		reel.scroll(1120);

		expect(reel.el.scrollTop).toBe(1120);
		reel.rest();
	});

	it('does not move when a second finger joins the swipe', () => {
		const reel = harness();
		reel.el.scrollTop = 800;

		reel.touch('touchstart', 1);
		reel.touch('touchstart', 2);
		reel.scroll(100_000);

		expect(reel.el.scrollTop).toBe(1600);
		reel.rest();
	});

	it('stays up while a finger rests mid-drag', () => {
		vi.useFakeTimers();
		const reel = harness();
		reel.el.scrollTop = 800;

		reel.touch('touchstart', 1);
		reel.scroll(900);
		vi.advanceTimersByTime(5000); // the thumb stops, and stays down
		reel.scroll(100_000);

		expect(reel.el.scrollTop).toBe(1600);
		reel.rest();
		vi.useRealTimers();
	});

	it('stays up while one finger of two lifts', () => {
		vi.useFakeTimers();
		const reel = harness();
		reel.el.scrollTop = 800;

		reel.touch('touchstart', 1);
		reel.touch('touchend', 1); // a finger left; one is still down
		vi.advanceTimersByTime(5000);
		reel.scroll(100_000);

		expect(reel.el.scrollTop).toBe(1600);
		reel.rest();
		vi.useRealTimers();
	});

	it('comes down once the swipe is over', () => {
		vi.useFakeTimers();
		const reel = harness();
		reel.el.scrollTop = 800;

		reel.touch('touchstart', 1);
		reel.touch('touchend', 0);
		vi.advanceTimersByTime(1000);

		// Not a gesture any more, so whatever moves the reel now is not held.
		reel.scroll(3200);

		expect(reel.el.scrollTop).toBe(3200);
		reel.rest();
		vi.useRealTimers();
	});

	it('never holds a move that was asked for', () => {
		const reel = harness();
		reel.el.scrollTop = 3200;
		reel.touch('touchstart', 1);

		reel.drive.to(0);

		expect(reel.el.scrollTop).toBe(0);
		reel.rest();
	});
});

describe('the wheel', () => {
	it('is one dream per gesture, and the momentum tail is not a second', () => {
		// Frozen, so the events of one gesture are one gesture's worth apart.
		vi.useFakeTimers();
		const reel = harness();

		reel.wheel(30); // under the reach, so nothing yet
		expect(reel.el.scrollTop).toBe(0);

		reel.wheel(30); // sixty in all, which is a swipe
		reel.settle();
		expect(reel.el.scrollTop).toBe(800);

		// The trackpad keeps talking for a second after the fingers have gone.
		for (let i = 0; i < 40; i++) reel.wheel(120);
		reel.settle();
		expect(reel.el.scrollTop).toBe(800);

		// A hand back on the wheel after a pause is a second gesture.
		vi.advanceTimersByTime(200);
		reel.wheel(120);
		reel.settle();
		expect(reel.el.scrollTop).toBe(1600);

		reel.rest();
		vi.useRealTimers();
	});

	it('goes back up, and stops at the top', () => {
		const reel = harness();
		reel.el.scrollTop = 800;

		reel.wheel(-200);
		reel.settle();

		expect(reel.el.scrollTop).toBe(0);
		reel.rest();
	});

	it('leaves a sideways gesture to the rail it belongs to', () => {
		const reel = harness();

		expect(reel.sideways()).toBe(false);
		expect(reel.el.scrollTop).toBe(0);
		reel.rest();
	});
});

describe('the keys', () => {
	it('move one dream, and reach both ends', () => {
		const reel = harness();

		reel.key('ArrowDown');
		reel.settle();
		expect(reel.el.scrollTop).toBe(800);

		reel.key('End');
		reel.settle();
		expect(reel.el.scrollTop).toBe(3200);

		reel.key('ArrowDown'); // already the last dream
		reel.settle();
		expect(reel.el.scrollTop).toBe(3200);

		reel.key('Home');
		reel.settle();
		expect(reel.el.scrollTop).toBe(0);
		reel.rest();
	});

	it('put the snapping back once the glide has landed', () => {
		const reel = harness();

		reel.key('ArrowDown');
		expect(reel.el.style.scrollSnapType).toBe('none');

		reel.settle();
		expect(reel.el.style.scrollSnapType).toBe('');
		reel.rest();
	});
});

describe('what the pager reports', () => {
	it('says which dream is on the screen, and says it once', () => {
		const reel = harness();

		reel.touch('touchstart', 1);
		reel.scroll(800);
		reel.scroll(820); // still the same dream
		reel.touch('touchend', 0);

		expect(reel.seen).toEqual([1]);
		reel.rest();
	});
});
