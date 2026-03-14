import { test, expect, type Page } from '@playwright/test';

const viewports = [
  { name: 'mobile', width: 375, height: 812 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'desktop', width: 1280, height: 800 },
];

const emptyPagedResponse = {
  items: [],
  page: 1,
  pageSize: 50,
  totalCount: 0,
  totalPages: 0,
};

const releaseListResponse = {
  items: [
    {
      id: 1,
      title: 'Playwright Test Release',
      releaseYear: '2020-01-01T00:00:00.000Z',
      origReleaseYear: '2020-01-01T00:00:00.000Z',
      artistNames: ['Playwright Artist'],
      genreNames: ['Test Genre'],
      labelName: 'Test Label',
      countryName: 'Test Country',
      formatName: 'Vinyl',
      coverImageUrl: '/placeholder-album.svg',
      dateAdded: '2024-01-01T00:00:00.000Z',
    },
  ],
  page: 1,
  pageSize: 60,
  totalCount: 1,
  totalPages: 1,
};

async function settleUi(page: Page) {
  // Wait for a couple of animation frames so layout settles before screenshots.
  await page.evaluate(async () => {
    await new Promise<void>((resolve) => {
      requestAnimationFrame(() => requestAnimationFrame(() => resolve()));
    });
  });
}

async function gotoCollectionAndWaitReady(
  page: Page,
  viewport: { width: number; height: number }
) {
  await page.setViewportSize({ width: viewport.width, height: viewport.height });
  await page.goto('/collection', { waitUntil: 'domcontentloaded' });

  // Ensure we are on the protected collection page and core shell rendered.
  await expect(page).toHaveURL(/\/collection(?:\?.*)?$/);
  await expect(page.locator('header').first()).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('button', { name: /^Filters$/ }).first()).toBeVisible({ timeout: 15000 });
  await expect(page.getByText('Playwright Test Release')).toBeVisible({ timeout: 15000 });

  await settleUi(page);
}

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    window.localStorage.setItem('auth_token', 'playwright-e2e-token');
  });

  await page.route('**/api/profile', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        userId: '00000000-0000-0000-0000-000000000001',
        email: 'playwright@example.com',
        displayName: 'Playwright User',
        selectedTheme: 'midnight',
        isAdmin: false,
      }),
    });
  });

  await page.route('**/api/health', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        status: 'Healthy',
        dbStatus: 'Healthy',
        timestamp: new Date().toISOString(),
        service: 'KollectorScum.Api',
        version: 'test',
      }),
    });
  });

  await page.route('**/api/kollections**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(emptyPagedResponse),
    });
  });

  await page.route(/\/api\/(artists|genres|labels|countries|formats)(\?.*)?$/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(emptyPagedResponse),
    });
  });

  await page.route('**/api/musicreleases/suggestions**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    });
  });

  await page.route('**/api/musicreleases**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(releaseListResponse),
    });
  });
});

for (const vp of viewports) {
  test.describe(`Visuals: ${vp.name}`, () => {
    test(`header snapshot - ${vp.name}`, async ({ page }) => {
      await gotoCollectionAndWaitReady(page, vp);

      // wait for header to render
      const header = page.locator('header').first();
      await expect(header).toBeVisible({ timeout: 15000 });

      // take a focused screenshot of the header for visual regression
      await expect(header).toHaveScreenshot(`${vp.name}-header.png`);
    });

    test(`advanced filters snapshot - ${vp.name}`, async ({ page }) => {
      await gotoCollectionAndWaitReady(page, vp);

      const filtersButton = page.getByRole('button', { name: /^Filters$/ }).first();
      await expect(filtersButton).toBeVisible({ timeout: 15000 });

      // Click Filters to open the advanced panel and wait for the URL to include the param
      await filtersButton.click();
      await expect(page).toHaveURL(/showAdvanced=true/);

      // Wait for some content in the advanced filters to show (e.g., the Artist label/text)
      // The advanced panel sets aria-hidden when closed, so look for the visible panel
      const advancedPanel = page.locator('[aria-hidden="false"]').first();
      await expect(advancedPanel).toBeVisible({ timeout: 15000 });
      await expect(advancedPanel.getByText('Artist', { exact: true })).toBeVisible({ timeout: 15000 });
      await expect(page.getByRole('button', { name: 'Close filters' })).toBeVisible({ timeout: 15000 });

      await settleUi(page);

      // capture the entire page to include the header and the opened advanced filters
      await expect(page).toHaveScreenshot(`${vp.name}-filters.png`, { fullPage: true });
    });
  });
}
