import { render, screen, act, waitFor } from '@testing-library/react';
import Header from '../Header';

jest.mock('../../lib/api', () => ({
  getPagedCount: jest.fn().mockResolvedValue(1234),
  getKollections: jest.fn().mockResolvedValue({ items: [] }),
  fetchJson: jest.fn().mockResolvedValue({}),
}));

// Provide simple next/navigation mocks used by Header
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/collection',
}));

describe('Header shrink-on-scroll', () => {
  let originalOffset: any;

  beforeEach(() => {
    // Make sure document default header height returns something predictable
    originalOffset = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'offsetHeight');
    Object.defineProperty(HTMLElement.prototype, 'offsetHeight', { configurable: true, get: () => 220 });
  });

  afterEach(() => {
    // restore original getter
    if (originalOffset) Object.defineProperty(HTMLElement.prototype, 'offsetHeight', originalOffset);
    jest.clearAllMocks();
  });

  it('renders title and subtitle and sets CSS header height', async () => {
    await act(async () => render(<Header />));

    // The header subtitle should be present
    expect(screen.getByText(/Organise and discover your music library/i)).toBeInTheDocument();

    // logo should be present at the top-left
    const logo = screen.getByAltText(/Kollector Sküm logo/i);
    expect(logo).toBeInTheDocument();
    // logo should be shown at its natural/intrinsic height (we don't force a height inline)
    expect((logo as HTMLElement).style.height).toBe('');

    // measured value should be applied to root css var
    await waitFor(() => {
      expect(document.documentElement.style.getPropertyValue('--app-header-height')).toBe('220px');
    });
  });

  it('sets compact class when scrolled past threshold and updates height', async () => {
    render(<Header />);

    const header = document.querySelector('header');
    expect(header).toBeTruthy();
    expect(header?.classList.contains('is-compact')).toBe(false);

    // Simulate the header becoming smaller after compacting
    Object.defineProperty(HTMLElement.prototype, 'offsetHeight', { configurable: true, get: () => 80 });

    // Simulate user scroll past threshold
    act(() => {
      (window as any).scrollY = 200;
      window.dispatchEvent(new Event('scroll'));
    });

    // rAF-based effect -> wait for DOM updates
    await waitFor(() => {
      expect(header?.classList.contains('is-compact')).toBe(true);
      expect(document.documentElement.style.getPropertyValue('--app-header-height')).toBe('80px');
    });
  });
});

describe('Header authentication visibility', () => {
  afterEach(() => {
    // Restore the auth token so other tests are unaffected
    window.localStorage.setItem('auth_token', 'test-token');
    jest.clearAllMocks();
  });

  it('does not render the header when the user is unauthenticated', async () => {
    window.localStorage.removeItem('auth_token');
    const { container } = render(<Header />);
    // Flush effects so the auth check runs
    await act(async () => {});
    expect(container.querySelector('header')).toBeNull();
  });

  it('renders the header when the user is authenticated', async () => {
    window.localStorage.setItem('auth_token', 'test-token');
    await act(async () => render(<Header />));
    expect(document.querySelector('header')).toBeTruthy();
  });

  it('hides the header when an authChanged event clears the token', async () => {
    window.localStorage.setItem('auth_token', 'test-token');
    await act(async () => render(<Header />));
    expect(document.querySelector('header')).toBeTruthy();

    // Simulate sign-out: clear token and fire authChanged
    await act(async () => {
      window.localStorage.removeItem('auth_token');
      window.dispatchEvent(new Event('authChanged'));
    });

    await waitFor(() => {
      expect(document.querySelector('header')).toBeNull();
    });
  });
});
// (Other header integration/unit tests live elsewhere in the repo.)

