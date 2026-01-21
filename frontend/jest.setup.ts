// Learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom'

// Provide a harmless global fetch mock for tests that render components which
// call fetch in useEffect during mount. Individual tests can override this mock
// when they need specific behavior.
if (typeof globalThis.fetch === 'undefined') {
	// eslint-disable-next-line @typescript-eslint/ban-ts-comment
	// @ts-ignore - Jest test environment global
	globalThis.fetch = jest.fn((input: any) => {
		const url = typeof input === 'string' ? input : input?.url ?? '';
		// Return a sensible default profile for components that validate
		// authentication during mount. Individual tests can override this
		// mock when they need specific behavior.
		if (url.includes('/api/profile')) {
			return Promise.resolve({
				ok: true,
				json: async () => ({
					email: 'test@example.com',
					name: 'Test User',
					hasCollection: true,
				}),
				headers: { get: () => 'application/json' },
			});
		}

		return Promise.resolve({
			ok: true,
			json: async () => ({}),
			headers: { get: () => 'application/json' },
		});
	});
}

// Mock Next App Router navigation hooks used by components. This prevents the
// "invariant expected app router to be mounted" errors in Jest where the
// Next app router isn't available. Tests can override these mocks if needed.
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
jest.mock('next/navigation', () => ({
	__esModule: true,
	useRouter: jest.fn(() => ({
		push: jest.fn(),
		replace: jest.fn(),
		back: jest.fn(),
		refresh: jest.fn(),
		prefetch: jest.fn().mockResolvedValue(undefined),
	})),
	usePathname: jest.fn(() => '/'),
	useSearchParams: jest.fn(() => new URLSearchParams()),
	useParams: jest.fn(() => ({})),
}));

// Provide a minimal mock for @react-oauth/google used by Header and auth flows.
// Tests don't need the real provider; ensure hooks and provider exist.
jest.mock('@react-oauth/google', () => ({
	__esModule: true,
	GoogleOAuthProvider: ({ children }: any) => children,
	useGoogleLogin: jest.fn(() => jest.fn()),
	useGoogleOneTapLogin: jest.fn(() => jest.fn()),
	useGoogleLogout: jest.fn(() => jest.fn()),
	useGoogleOAuth: jest.fn(() => ({})),
	GoogleLogin: jest.fn(() => null),
}));

// Ensure NEXT_PUBLIC_GOOGLE_CLIENT_ID is set for tests that require it.
process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID || 'test-google-client-id';

// Polyfill ResizeObserver for the Jest/jsdom environment used by tests.
// Many components measure layout using ResizeObserver; provide a minimal
// no-op implementation so tests can run without the real browser API.
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
global.ResizeObserver = global.ResizeObserver || class {
	cb: any;
	constructor(cb: any) { this.cb = cb; }
	observe() { /* no-op */ }
	unobserve() { /* no-op */ }
	disconnect() { /* no-op */ }
};

// Ensure tests run with an authenticated user by default so components that
// depend on `isAuthenticated()` render the authenticated UI. Individual tests
// can clear this value if they need an unauthenticated scenario.
try {
	// Ensure we set the auth token using the jsdom `window.localStorage` when
	// running under Jest. Accessing the top-level `localStorage` may be
	// undefined in some environments, so prefer `window` or `globalThis`.
	// eslint-disable-next-line @typescript-eslint/ban-ts-comment
	// @ts-ignore
	if (typeof window !== 'undefined' && window.localStorage) {
		// eslint-disable-next-line @typescript-eslint/ban-ts-comment
		// @ts-ignore
		window.localStorage.setItem('auth_token', 'test-token');
	} else if (typeof globalThis !== 'undefined' && (globalThis as any).localStorage) {
		(globalThis as any).localStorage.setItem('auth_token', 'test-token');
	}
} catch (e) {}

// Debug: confirm localStorage token presence when running tests
try {
	// eslint-disable-next-line @typescript-eslint/ban-ts-comment
	// @ts-ignore
	if (typeof window !== 'undefined' && window.localStorage) {
		 
		console.log('[jest.setup] auth_token =', window.localStorage.getItem('auth_token'));
	}
} catch (e) {}

// Ensure each test starts with a test auth token so components under test
// treat the environment as authenticated unless the test explicitly clears it.
beforeEach(() => {
	try {
		// eslint-disable-next-line @typescript-eslint/ban-ts-comment
		// @ts-ignore
		if (typeof window !== 'undefined' && window.localStorage) {
			// eslint-disable-next-line @typescript-eslint/ban-ts-comment
			// @ts-ignore
			window.localStorage.setItem('auth_token', 'test-token');
			 
			console.log('[jest.setup.beforeEach] auth_token =', window.localStorage.getItem('auth_token'));
		}
	} catch (e) {}
});

// NOTE: Previously we reset the module registry before each test to avoid
// cross-test leakage. That interferes with test-level `jest.mock`/`doMock`
// registrations in some suites (Header.responsive). Removing the global
// reset keeps per-test mocks reliable while tests remain isolated.

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
