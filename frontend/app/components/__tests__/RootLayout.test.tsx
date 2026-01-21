import React from 'react';
import { renderToStaticMarkup } from 'react-dom/server';

// Mock child components used by layout to control complexity
jest.mock('../../components/Sidebar', () => ({
  __esModule: true,
  default: () => <div data-testid="sidebar">Sidebar</div>,
}));

jest.mock('../../components/Header', () => ({
  __esModule: true,
  default: () => <header data-testid="header">Header</header>,
}));

jest.mock('../../components/Footer', () => ({
  __esModule: true,
  default: () => <footer data-testid="footer">Footer</footer>,
}));

jest.mock('../../components/ErrorBoundary', () => ({
  __esModule: true,
  ErrorBoundary: ({ children }: any) => <div>{children}</div>,
}));

import RootLayout from '../../layout';

describe('RootLayout layout container', () => {
  it('uses the CSS variable for content margin-left', () => {
    // Server-render the layout to avoid nesting <html> inside document body in test DOM
    const html = renderToStaticMarkup(<RootLayout>{<div>child</div>}</RootLayout>);
    expect(html).toContain('app-scroll-container');
    // The style attribute should contain the margin-left variable (no guaranteed spacing)
    expect(/margin-left:\s*var\(--sidebar-offset,\s*64px\)/.test(html) || /margin-left:var\(--sidebar-offset,64px\)/.test(html)).toBeTruthy();
  });
});
