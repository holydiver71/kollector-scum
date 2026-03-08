import React from 'react';
import { render, screen } from '@testing-library/react';
import { IntroPage } from '../IntroPage';

// Mock GoogleSignIn so we can assert it is rendered without triggering real auth flows
jest.mock('../GoogleSignIn', () => ({
  GoogleSignIn: () => <button data-testid="google-sign-in-btn">Sign in with Google</button>,
}));

// LoadingSpinner is used during the loading state
jest.mock('../LoadingComponents', () => ({
  LoadingSpinner: () => <div data-testid="loading-spinner" />,
}));

describe('IntroPage', () => {
  it('renders the logo image', () => {
    render(<IntroPage />);
    expect(screen.getByAltText(/Kollector Sküm Logo/i)).toBeInTheDocument();
  });

  it('renders the tagline text', () => {
    render(<IntroPage />);
    expect(screen.getByText(/The Ultimate Hub for Your Physical Media/i)).toBeInTheDocument();
  });

  it('renders the feature highlights grid', () => {
    render(<IntroPage />);
    expect(screen.getByText(/Track Everything/i)).toBeInTheDocument();
    expect(screen.getByText(/Deep Insights/i)).toBeInTheDocument();
    expect(screen.getByText(/Dark & Sleek/i)).toBeInTheDocument();
  });

  it('renders the "Ready to spin?" CTA heading', () => {
    render(<IntroPage />);
    expect(screen.getByText(/Ready to spin\?/i)).toBeInTheDocument();
  });

  it('renders updated CTA description without top-right-corner reference', () => {
    render(<IntroPage />);
    const desc = screen.getByText(/Sign in with Google to get started and manage your music collection/i);
    expect(desc).toBeInTheDocument();
    expect(desc.textContent).not.toMatch(/top right corner/i);
  });

  it('renders the Google Sign-in button centered below the CTA heading', () => {
    const { container } = render(<IntroPage />);
    const signInBtn = screen.getByTestId('google-sign-in-btn');
    expect(signInBtn).toBeInTheDocument();

    // The button should be wrapped in a flex justify-center container
    const wrapper = signInBtn.parentElement;
    expect(wrapper).toHaveClass('flex', 'justify-center');
  });

  it('shows loading spinner instead of CTA when loading prop is true', () => {
    render(<IntroPage loading />);
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
    expect(screen.queryByTestId('google-sign-in-btn')).not.toBeInTheDocument();
    expect(screen.queryByText(/Ready to spin\?/i)).not.toBeInTheDocument();
  });
});
