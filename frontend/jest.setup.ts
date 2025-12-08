// Learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom'

// Provide a harmless global fetch mock for tests that render components which
// call fetch in useEffect during mount. Individual tests can override this mock
// when they need specific behavior.
if (typeof globalThis.fetch === 'undefined') {
	// eslint-disable-next-line @typescript-eslint/ban-ts-comment
	// @ts-ignore - Jest test environment global
	globalThis.fetch = jest.fn(() =>
		Promise.resolve({
			ok: true,
			json: async () => ({}),
			headers: { get: () => 'application/json' },
		})
	);
}

// Quiet noisy React testing warnings that we intentionally accept for some
// components that perform background async work during mount in tests.
// We still allow other console.error messages through.
const _origConsoleError = console.error.bind(console);
console.error = (...args: any[]) => {
	const first = args[0] as string | undefined;
	if (typeof first === 'string') {
		// Suppress 'not wrapped in act' warnings in the test logs (tests are
		// already written to be robust, and these warnings were noisy during CI).
		if (first.includes('not wrapped in act')) return;
		// Suppress fetch/setState noise that occurs when components run background
		// fetches in tests and we intentionally rely on local mocks
		if (first.includes('Failed to fetch') || first.includes('Error fetching')) return;
	}
	_origConsoleError(...args);
};
