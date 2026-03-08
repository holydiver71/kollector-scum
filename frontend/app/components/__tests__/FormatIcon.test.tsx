import { render } from '@testing-library/react';
import { FormatIcon } from '../FormatIcon';

describe('FormatIcon', () => {
  it('returns null when no formatName is provided', () => {
    const { container } = render(<FormatIcon />);
    expect(container.firstChild).toBeNull();
  });

  const testCases = [
    { name: 'CD Single', formatName: 'CD Single', expectedText: 'S', gradientRef: 'cd-single-gradient' },
    { name: 'Maxi-CD', formatName: 'Maxi-CD', expectedText: 'S', gradientRef: 'cd-single-gradient' },
    { name: 'CD-R', formatName: 'CD-R', expectedText: 'R', gradientRef: 'cdr-gradient' },
    { name: 'CD ROM', formatName: 'CD ROM', expectedText: 'R', gradientRef: 'cdr-gradient' },
    { name: 'Standard CD', formatName: 'CD', notExpectedText: ['S', 'R'], gradientRef: 'cd-gradient' },
    { name: 'Cassette', formatName: 'Cassette' },
    { name: '7" Vinyl', formatName: '7" Single' },
    { name: '10" Vinyl', formatName: '10"' },
    { name: '12" Vinyl', formatName: '12"' },
    { name: 'LP', formatName: 'LP' },
    { name: 'Unknown Format', formatName: 'Unknown', fallback: true }
  ];

  testCases.forEach((tc) => {
    it(`renders correctly for ${tc.name}`, () => {
      const { container, getByText } = render(<FormatIcon formatName={tc.formatName} />);
      
      const svg = container.querySelector('svg');
      expect(svg).toBeInTheDocument();
      
      if (tc.expectedText) {
        expect(getByText(tc.expectedText)).toBeInTheDocument();
      }
      
      if (tc.notExpectedText) {
        tc.notExpectedText.forEach(text => {
          expect(container.textContent).not.toContain(text);
        });
      }
      
      if (tc.gradientRef) {
        expect(container.innerHTML).toContain(`url(#${tc.gradientRef})`);
      }

      if (tc.fallback) {
        expect(getByText('?')).toBeInTheDocument();
      }
    });
  });

  it('applies custom className correctly', () => {
    const { container } = render(<FormatIcon formatName="CD" className="custom-class" />);
    expect(container.firstChild).toHaveClass('custom-class');
  });
});
