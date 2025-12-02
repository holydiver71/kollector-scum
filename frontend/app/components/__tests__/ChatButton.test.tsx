import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import ChatButton from '../ChatButton';

describe('ChatButton Component', () => {
  describe('Initial State', () => {
    it('renders the chat button', () => {
      render(<ChatButton />);
      const button = screen.getByTestId('chat-button');
      expect(button).toBeInTheDocument();
    });

    it('renders the chat icon', () => {
      render(<ChatButton />);
      const button = screen.getByTestId('chat-button');
      const svg = button.querySelector('svg');
      expect(svg).toBeInTheDocument();
    });

    it('does not show chat window initially', () => {
      render(<ChatButton />);
      expect(screen.queryByTestId('chat-window')).not.toBeInTheDocument();
    });

    it('has accessible label', () => {
      render(<ChatButton />);
      const button = screen.getByLabelText('Open chat');
      expect(button).toBeInTheDocument();
    });
  });

  describe('Opening Chat Window', () => {
    it('opens chat window when button is clicked', () => {
      render(<ChatButton />);
      const button = screen.getByTestId('chat-button');
      
      fireEvent.click(button);
      
      expect(screen.getByTestId('chat-window')).toBeInTheDocument();
    });

    it('displays chat title in header', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      expect(screen.getByText('Kollector Sküm Chat')).toBeInTheDocument();
    });

    it('displays custom title when provided', () => {
      render(<ChatButton title="Custom Chat Title" />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      expect(screen.getByText('Custom Chat Title')).toBeInTheDocument();
    });

    it('shows welcome message', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      expect(screen.getByText(/Welcome to Kollector Sküm Chat/)).toBeInTheDocument();
    });

    it('displays messages area', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      expect(screen.getByTestId('chat-messages')).toBeInTheDocument();
    });

    it('displays chat input form', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      expect(screen.getByTestId('chat-form')).toBeInTheDocument();
      expect(screen.getByTestId('chat-input')).toBeInTheDocument();
      expect(screen.getByTestId('chat-send-button')).toBeInTheDocument();
    });
  });

  describe('Closing Chat Window', () => {
    it('closes chat window when close button is clicked', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      expect(screen.getByTestId('chat-window')).toBeInTheDocument();
      
      fireEvent.click(screen.getByTestId('chat-close-button'));
      
      expect(screen.queryByTestId('chat-window')).not.toBeInTheDocument();
    });

    it('closes chat window when overlay is clicked', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      expect(screen.getByTestId('chat-window')).toBeInTheDocument();
      
      fireEvent.click(screen.getByTestId('chat-overlay'));
      
      expect(screen.queryByTestId('chat-window')).not.toBeInTheDocument();
    });

    it('does not close when chat window content is clicked', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      fireEvent.click(screen.getByTestId('chat-window'));
      
      expect(screen.getByTestId('chat-window')).toBeInTheDocument();
    });

    it('close button has accessible label', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const closeButton = screen.getByLabelText('Close chat');
      expect(closeButton).toBeInTheDocument();
    });
  });

  describe('Sending Messages', () => {
    it('disables send button when input is empty', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const sendButton = screen.getByTestId('chat-send-button');
      expect(sendButton).toBeDisabled();
    });

    it('enables send button when input has content', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const input = screen.getByTestId('chat-input');
      fireEvent.change(input, { target: { value: 'Hello' } });
      
      const sendButton = screen.getByTestId('chat-send-button');
      expect(sendButton).not.toBeDisabled();
    });

    it('sends message when form is submitted', async () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const input = screen.getByTestId('chat-input');
      fireEvent.change(input, { target: { value: 'Hello World' } });
      
      const form = screen.getByTestId('chat-form');
      fireEvent.submit(form);
      
      expect(screen.getByText('Hello World')).toBeInTheDocument();
    });

    it('clears input after sending message', async () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const input = screen.getByTestId('chat-input') as HTMLInputElement;
      fireEvent.change(input, { target: { value: 'Hello World' } });
      
      const form = screen.getByTestId('chat-form');
      fireEvent.submit(form);
      
      expect(input.value).toBe('');
    });

    it('receives system response after sending message', async () => {
      jest.useFakeTimers();
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const input = screen.getByTestId('chat-input');
      fireEvent.change(input, { target: { value: 'Test message' } });
      
      const form = screen.getByTestId('chat-form');
      fireEvent.submit(form);
      
      // Fast-forward past the 1 second delay for system response
      jest.advanceTimersByTime(1500);
      
      await waitFor(() => {
        expect(screen.getByText(/placeholder response/)).toBeInTheDocument();
      });
      
      jest.useRealTimers();
    });

    it('does not send message with only whitespace', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const input = screen.getByTestId('chat-input');
      fireEvent.change(input, { target: { value: '   ' } });
      
      const sendButton = screen.getByTestId('chat-send-button');
      expect(sendButton).toBeDisabled();
    });
  });

  describe('Input Interaction', () => {
    it('focuses input when chat opens', async () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      await waitFor(() => {
        const input = screen.getByTestId('chat-input');
        expect(input).toHaveFocus();
      });
    });

    it('allows typing in input field', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const input = screen.getByTestId('chat-input') as HTMLInputElement;
      fireEvent.change(input, { target: { value: 'Test input' } });
      
      expect(input.value).toBe('Test input');
    });

    it('has placeholder text', () => {
      render(<ChatButton />);
      fireEvent.click(screen.getByTestId('chat-button'));
      
      const input = screen.getByPlaceholderText('Type your message...');
      expect(input).toBeInTheDocument();
    });
  });

  describe('Toggle Functionality', () => {
    it('toggles chat window open and closed', () => {
      render(<ChatButton />);
      const button = screen.getByTestId('chat-button');
      
      // First click opens
      fireEvent.click(button);
      expect(screen.getByTestId('chat-window')).toBeInTheDocument();
      
      // Second click closes
      fireEvent.click(button);
      expect(screen.queryByTestId('chat-window')).not.toBeInTheDocument();
    });
  });
});
