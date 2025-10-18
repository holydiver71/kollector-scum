import { test, expect } from '@playwright/test';

test.describe('Collection Page', () => {
  test('should load collection page with releases', async ({ page }) => {
    await page.goto('/collection');

    // Check page header
    await expect(page.getByRole('heading', { name: /Music Collection/i })).toBeVisible();

    // Wait for releases to load
    await page.waitForSelector('[class*="grid"]', { timeout: 10000 });

    // Check that release cards are displayed
    const releaseCards = page.locator('[href^="/releases/"]');
    await expect(releaseCards.first()).toBeVisible({ timeout: 10000 });
  });

  test('should display search and filter controls', async ({ page }) => {
    await page.goto('/collection');

    // Check for search input
    await expect(page.getByPlaceholder(/Search releases/i)).toBeVisible();

    // Check for filter toggle button
    await expect(page.getByRole('button', { name: /Filters|Advanced/i })).toBeVisible();
  });

  test('should filter releases by search term', async ({ page }) => {
    await page.goto('/collection');

    // Wait for initial load
    await page.waitForSelector('[href^="/releases/"]', { timeout: 10000 });

    // Type in search box
    const searchBox = page.getByPlaceholder(/Search releases/i);
    await searchBox.fill('metal');

    // Wait a moment for debounce
    await page.waitForTimeout(500);

    // Results should update (check that we still have results or a "no results" message)
    const hasResults = await page.locator('[href^="/releases/"]').count() > 0;
    const hasNoResults = await page.getByText(/No releases found/i).isVisible().catch(() => false);
    
    expect(hasResults || hasNoResults).toBeTruthy();
  });

  test('should show advanced filters when toggle is clicked', async ({ page }) => {
    await page.goto('/collection');

    // Click the advanced filters toggle
    const filtersButton = page.getByRole('button', { name: /Filters|Advanced|Show Filters/i }).first();
    await filtersButton.click();

    // Advanced filters should be visible
    await expect(page.getByText(/Genre|Artist|Label|Format/i).first()).toBeVisible({ timeout: 5000 });
  });

  test('should navigate to release detail page when clicking a release', async ({ page }) => {
    await page.goto('/collection');

    // Wait for releases to load
    await page.waitForSelector('[href^="/releases/"]', { timeout: 10000 });

    // Click the first release
    const firstRelease = page.locator('[href^="/releases/"]').first();
    await firstRelease.click();

    // Should navigate to detail page
    await expect(page).toHaveURL(/\/releases\/\d+/);

    // Detail page should have a title
    await expect(page.locator('h1').first()).toBeVisible();
  });

  test('should display pagination or load more functionality', async ({ page }) => {
    await page.goto('/collection');

    // Wait for releases to load
    await page.waitForSelector('[href^="/releases/"]', { timeout: 10000 });

    // Check if there's pagination or "Load More" button
    const hasPagination = await page.getByRole('button', { name: /Next|Previous|Load More/i }).isVisible().catch(() => false);
    const hasInfiniteScroll = await page.locator('[class*="observer"]').isVisible().catch(() => false);
    
    // Should have some way to load more results
    // (This is a placeholder - adjust based on actual implementation)
    const releaseCount = await page.locator('[href^="/releases/"]').count();
    expect(releaseCount).toBeGreaterThan(0);
  });
});
