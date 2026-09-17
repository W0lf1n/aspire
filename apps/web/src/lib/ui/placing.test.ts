import { describe, expect, it } from 'vitest';
import { CENTRED, type Focal } from '$lib/images/focal';
import { NUDGE, hands } from './placing';

/**
 * A frame half as wide as it is tall, and a picture twice as wide as it is
 * tall: filling, the picture is 400 × 200 in a 100 × 200 frame, so it hangs
 * 300 px over across and not at all down.
 */
function setup(resting = false) {
	let now: Focal = { ...CENTRED };
	const placed = hands({
		get: () => now,
		set: (focal) => (now = focal),
		frame: () => ({ width: 100, height: 200 }),
		picture: () => ({ width: 400, height: 200 }),
		resting: () => resting
	});
	return { placed, now: () => now };
}

const finger = (pointerId: number, clientX: number, clientY = 0) => ({
	pointerId,
	clientX,
	clientY
});
const quiet = { preventDefault: () => {} };

describe('hands', () => {
	it('moves the point against a dragging finger, measured from where it went down', () => {
		const { placed, now } = setup();

		placed.down(finger(1, 50));
		placed.move(finger(1, 80));
		placed.move(finger(1, 110));

		// 60 px of 300 px of overhang is a fifth of the picture, to the left.
		expect(now().x).toBeCloseTo(0.3);
		expect(now().y).toBe(0.5);
	});

	it('zooms by how far two fingers have spread since they landed', () => {
		const { placed, now } = setup();

		placed.down(finger(1, 0));
		placed.down(finger(2, 100));
		placed.move(finger(2, 200));

		expect(now().zoom).toBeCloseTo(2);
	});

	it('does not jump when one finger of a pinch is lifted and the other moves on', () => {
		const { placed, now } = setup();

		placed.down(finger(1, 0));
		placed.down(finger(2, 100));
		placed.move(finger(2, 150));
		placed.up(finger(2, 150));
		const pinched = now();
		placed.move(finger(1, 0));

		expect(now()).toEqual(pinched);
		expect(placed.holding()).toBe(true);

		placed.up(finger(1, 0));
		expect(placed.holding()).toBe(false);
	});

	it('ignores a finger that is not down', () => {
		const { placed, now } = setup();

		placed.move(finger(7, 90));

		expect(now()).toEqual(CENTRED);
	});

	it('does not start a gesture while the editor is saving', () => {
		const { placed, now } = setup(true);

		placed.down(finger(1, 50));
		placed.move(finger(1, 110));

		expect(now()).toEqual(CENTRED);
		expect(placed.holding()).toBe(false);
	});

	it('nudges with the arrows and zooms with plus and minus', () => {
		const { placed, now } = setup();

		placed.keys({ key: 'ArrowRight', ...quiet });
		expect(now().x).toBeCloseTo(0.5 - NUDGE / 300);

		placed.keys({ key: '+', ...quiet });
		expect(now().zoom).toBeCloseTo(1.1);

		placed.keys({ key: '-', ...quiet });
		expect(now().zoom).toBeCloseTo(1);
	});

	it('zooms in on a wheel turned away, and out on one turned back', () => {
		const { placed, now } = setup();

		placed.wheel({ deltaY: -1, ...quiet });
		expect(now().zoom).toBeCloseTo(1.08);

		placed.wheel({ deltaY: 1, ...quiet });
		expect(now().zoom).toBeCloseTo(1);
	});
});
