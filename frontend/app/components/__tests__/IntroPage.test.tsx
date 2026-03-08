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

  it('does not render a "Ready to spin?" heading', () => {
    render(<IntroPage />);
    expect(screen.queryByText(/Ready to spin\?/i)).not.toBeInTheDocument();
  });

  it('renders the Google Sign-in button centered below the description text', () => {
    const { container } = render(<IntroPage />);
    const signInBtn = screen.getByTestId('google-sign-in-btn');
    expect(signInBtn).toBeInTheDocument();

    // The button should be wrapped in a flex justify-center container
    const wrapper = signInBtn.parentElement;
    expect(wrapper).toHaveClass('flex', 'justify-center');
  });

  it('renders the Sign-in button before the feature highlights grid', () => {
    const { container } = render(<IntroPage />);
    const signInBtn = screen.getByTestId('google-sign-in-btn');
    const featureHeading = screen.getByText(/Track Everything/i);

    // compareDocumentPosition: DOCUMENT_POSITION_FOLLOWING = 4 means signInBtn comes before featureHeading
    const position = signInBtn.compareDocumentPosition(featureHeading);
    expect(position & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('shows loading spinner instead of sign-in button when loading prop is true', () => {
    render(<IntroPage loading />);
    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
    expect(screen.queryByTestId('google-sign-in-btn')).not.toBeInTheDocument();
  });
});
