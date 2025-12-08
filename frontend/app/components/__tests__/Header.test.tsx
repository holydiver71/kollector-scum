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

    // logo should be present at the top-left and be offset by the sidebar
    const logo = screen.getByAltText(/Kollector Sküm logo/i);
    expect(logo).toBeInTheDocument();
    // border should be present and visible so the occupied area is obvious
    expect(logo).toHaveClass('border-2', 'border-white');
    // logo should be shown at its natural/intrinsic height (we don't force a height inline)
    expect((logo as HTMLElement).style.height).toBe('');
    // the top-bar wrapper should be fixed and offset using the --sidebar-offset CSS var
    const topBar = document.querySelector('.fixed.top-0.right-0');
    expect(topBar).toBeTruthy();
    // style.left should contain the CSS variable reference so it stays next to the sidebar
    expect((topBar as HTMLElement).style.left).toBe('var(--sidebar-offset)');
    // the top bar should have no top padding and align items to the top so the logo's top is at page top
    const topInner = topBar?.firstElementChild as HTMLElement | null;
    expect(topInner).toBeTruthy();
    expect(topInner).toHaveClass('py-0', 'items-start');

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
