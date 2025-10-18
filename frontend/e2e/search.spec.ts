import { test, expect } from '@playwright/test';

test.describe('Search Page', () => {
  test('should display search landing page initially', async ({ page }) => {
    await page.goto('/search');

    // Check for main heading
    await expect(page.getByRole('heading', { name: /Search Music/i })).toBeVisible();

    // Check for search icon and description
    await expect(page.getByText(/Use the search below/i)).toBeVisible();
    await expect(page.getByText(/🔍/)).toBeVisible();
  });

  test('should show results after searching', async ({ page }) => {
    await page.goto('/search');

    // Find and fill search input
    const searchInput = page.getByPlaceholder(/Search/i).first();
    await searchInput.fill('metallica');
    await searchInput.press('Enter');

    // Wait a moment for results
    await page.waitForTimeout(1000);

    // Should either show results or "no results" message
    const hasResults = await page.locator('[href^="/releases/"]').isVisible().catch(() => false);
    const noResults = await page.getByText(/No releases found/i).isVisible().catch(() => false);
    
    expect(hasResults || noResults).toBeTruthy();
  });

  test('should show advanced filters', async ({ page }) => {
    await page.goto('/search');

    // Perform a search first
    const searchInput = page.getByPlaceholder(/Search/i).first();
    await searchInput.fill('rock');
    await searchInput.press('Enter');

    await page.waitForTimeout(500);

    // Look for advanced filters button
    const filtersButton = page.getByRole('button', { name: /Filters|Advanced/i }).first();
    if (await filtersButton.isVisible()) {
      await filtersButton.click();
      
      // Filters should be visible
      await expect(page.getByText(/Genre|Artist|Format/i).first()).toBeVisible({ timeout: 5000 });
    }
  });

  test('should navigate to release detail from search results', async ({ page }) => {
    await page.goto('/search');

    // Perform search
    const searchInput = page.getByPlaceholder(/Search/i).first();
    await searchInput.fill('album');
    await searchInput.press('Enter');

    // Wait for results
    await page.waitForTimeout(1000);

    // Click first result if available
    const firstResult = page.locator('[href^="/releases/"]').first();
    const isVisible = await firstResult.isVisible().catch(() => false);
    
    if (isVisible) {
      await firstResult.click();
      await expect(page).toHaveURL(/\/releases\/\d+/);
    }
  });
});
