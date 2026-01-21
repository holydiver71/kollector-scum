import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import Sidebar from '../Sidebar';

// Mock next/navigation used by Sidebar
jest.mock('next/navigation', () => ({
  usePathname: () => '/',
}));

describe('Sidebar ↔ Header offset sync', () => {
  beforeEach(() => {
    // Ensure no leftover value
    document.documentElement.style.removeProperty('--sidebar-offset');
  });

  afterEach(() => {
    document.documentElement.style.removeProperty('--sidebar-offset');
    jest.resetAllMocks();
  });

  it('sets default offset and updates when toggled', async () => {
    render(<Sidebar />);

    // default collapsed offset should be set
    await waitFor(() => {
      expect(document.documentElement.style.getPropertyValue('--sidebar-offset')).toBe('64px');
    });

    // Toggle the button (first button in sidebar is the toggle)
    const toggle = screen.getAllByRole('button')[0];
    fireEvent.click(toggle);

    await waitFor(() => {
      expect(document.documentElement.style.getPropertyValue('--sidebar-offset')).toBe('240px');
    });

    // Toggle back
    fireEvent.click(toggle);
    await waitFor(() => {
      expect(document.documentElement.style.getPropertyValue('--sidebar-offset')).toBe('64px');
    });
  });

  it('does not change offset on hover — only toggle button should control expansion', async () => {
    const { container } = render(<Sidebar />);

    // default collapsed offset should be set
    await waitFor(() => {
      expect(document.documentElement.style.getPropertyValue('--sidebar-offset')).toBe('64px');
    });

    const aside = container.querySelector('aside');
    expect(aside).toBeTruthy();

    // confirm collapsed class applied and nav prevents horizontal overflow
    expect(aside?.classList.contains('sidebar-collapsed')).toBe(true);
    const nav = container.querySelector('nav');
    expect(nav).toBeTruthy();
    expect(nav?.className).toEqual(expect.stringContaining('overflow-x-hidden'));

    // Simulate mouse enter / leave — since hover-driven expansion was removed, offset must remain unchanged
    if (aside) {
      fireEvent.mouseEnter(aside);
      await new Promise((r) => setTimeout(r, 50));
      expect(document.documentElement.style.getPropertyValue('--sidebar-offset')).toBe('64px');

      fireEvent.mouseLeave(aside);
      await new Promise((r) => setTimeout(r, 50));
      expect(document.documentElement.style.getPropertyValue('--sidebar-offset')).toBe('64px');
    }

    // Ensure clicking the toggle still works
    const toggle = screen.getAllByRole('button')[0];
    fireEvent.click(toggle);

    await waitFor(() => {
      expect(document.documentElement.style.getPropertyValue('--sidebar-offset')).toBe('240px');
    });
  });
});
