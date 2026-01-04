test('import header', () => {
  const Header = require('../Header').default;
  expect(typeof Header).toBe('function');
});
