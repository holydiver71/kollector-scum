import { test, expect } from '@playwright/test';

const viewports = [
  { name: 'mobile', width: 375, height: 812 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'desktop', width: 1280, height: 800 },
];

for (const vp of viewports) {
  test.describe.parallel(`Visuals: ${vp.name}`, () => {
    test(`header snapshot - ${vp.name}`, async ({ page, baseURL }) => {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.goto('/collection');

      // wait for header to render
      const header = page.locator('header.header-with-bg');
      await header.waitFor({ state: 'visible' });

      // take a focused screenshot of the header for visual regression
      await expect(header).toHaveScreenshot(`${vp.name}-header.png`);
    });

    test(`advanced filters snapshot - ${vp.name}`, async ({ page }) => {
      await page.setViewportSize({ width: vp.width, height: vp.height });
      await page.goto('/collection');

      // Click Filters to open the advanced panel and wait for the URL to include the param
      await page.getByRole('button', { name: /Filters/i }).click();
      await page.waitForURL(/\?showAdvanced=true|showAdvanced=true/);

      // Wait for some content in the advanced filters to show (e.g., the Artist label/text)
      // The advanced panel sets aria-hidden when closed, so look for the visible panel
      await page.locator('[aria-hidden="false"]').getByText('Artist', { exact: true }).waitFor({ state: 'visible' });

      // capture the entire page to include the header and the opened advanced filters
      await expect(page).toHaveScreenshot(`${vp.name}-filters.png`, { fullPage: true });
    });
  });
}
