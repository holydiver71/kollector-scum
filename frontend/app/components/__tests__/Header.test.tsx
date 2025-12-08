import { render, screen, act, waitFor } from '@testing-library/react';
import Header from '../Header';

jest.mock('../../lib/api', () => ({
  getPagedCount: jest.fn().mockResolvedValue(1234),
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

    // The main title should be present as the primary heading
    const heading = screen.getByRole('heading', { level: 1 });
    expect(heading).toBeInTheDocument();

    // logo should be present to the left of the title
    const logo = screen.getByAltText(/Kollector Sküm logo/i);
    expect(logo).toBeInTheDocument();

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
// (Other header integration/unit tests live elsewhere in the repo.)
