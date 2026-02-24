import React from 'react';
import { render, screen, act, fireEvent } from '@testing-library/react';
import { ThemeProvider, useTheme, AVAILABLE_THEMES } from '../../contexts/ThemeContext';

// Helper component that exposes the context values for testing
function ThemeConsumer() {
  const { theme, setTheme } = useTheme();
  return (
    <div>
      <span data-testid="current-theme">{theme}</span>
      {AVAILABLE_THEMES.map((t) => (
        <button
          key={t.name}
          data-testid={`set-theme-${t.name}`}
          onClick={() => setTheme(t.name)}
        >
          {t.label}
        </button>
      ))}
    </div>
  );
}

describe('ThemeProvider', () => {
  beforeEach(() => {
    localStorage.clear();
    // Reset data-theme attribute
    document.documentElement.removeAttribute('data-theme');
  });

  it('provides the default theme "metal-default"', () => {
    render(
      <ThemeProvider>
        <ThemeConsumer />
      </ThemeProvider>
    );
    expect(screen.getByTestId('current-theme').textContent).toBe('metal-default');
  });

  it('applies data-theme attribute to <html> on mount', () => {
    render(
      <ThemeProvider>
        <ThemeConsumer />
      </ThemeProvider>
    );
    expect(document.documentElement.getAttribute('data-theme')).toBe('metal-default');
  });

  it('updates the theme when setTheme is called', () => {
    render(
      <ThemeProvider>
        <ThemeConsumer />
      </ThemeProvider>
    );

    act(() => {
      fireEvent.click(screen.getByTestId('set-theme-clean-light'));
    });

    expect(screen.getByTestId('current-theme').textContent).toBe('clean-light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('clean-light');
  });

  it('persists the selected theme to localStorage', () => {
    render(
      <ThemeProvider>
        <ThemeConsumer />
      </ThemeProvider>
    );

    act(() => {
      fireEvent.click(screen.getByTestId('set-theme-clean-light'));
    });

    expect(localStorage.getItem('selectedTheme')).toBe('clean-light');
  });

  it('restores theme from localStorage on mount', () => {
    localStorage.setItem('selectedTheme', 'clean-light');

    render(
      <ThemeProvider>
        <ThemeConsumer />
      </ThemeProvider>
    );

    expect(screen.getByTestId('current-theme').textContent).toBe('clean-light');
  });

  it('ignores unknown theme values stored in localStorage', () => {
    localStorage.setItem('selectedTheme', 'unknown-theme');

    render(
      <ThemeProvider>
        <ThemeConsumer />
      </ThemeProvider>
    );

    // Should fall back to default
    expect(screen.getByTestId('current-theme').textContent).toBe('metal-default');
  });
});

describe('AVAILABLE_THEMES', () => {
  it('contains metal-default', () => {
    expect(AVAILABLE_THEMES.some((t) => t.name === 'metal-default')).toBe(true);
  });

  it('contains clean-light', () => {
    expect(AVAILABLE_THEMES.some((t) => t.name === 'clean-light')).toBe(true);
  });
});
