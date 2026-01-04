import React from 'react';
import { render } from '@testing-library/react';

test('hook smoke', () => {
  function Comp() {
    const r = React.useRef(null);
    return <div data-testid="ok" />;
  }

  const { getByTestId } = render(<Comp />);
  expect(getByTestId('ok')).toBeTruthy();
});
