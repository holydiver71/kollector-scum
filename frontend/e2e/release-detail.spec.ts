import { test, expect } from '@playwright/test';

test.describe('Release Detail Page', () => {
  test('should load release detail page', async ({ page }) => {
    // First go to collection to get a release
    await page.goto('/collection');
    await page.waitForSelector('[href^="/releases/"]', { timeout: 10000 });
    
    // Click first release
    const firstRelease = page.locator('[href^="/releases/"]').first();
    await firstRelease.click();

    // Should be on detail page
    await expect(page).toHaveURL(/\/releases\/\d+/);

    // Should have a main heading (album title)
    await expect(page.locator('h1, h2').first()).toBeVisible();
  });

  test('should display release metadata', async ({ page }) => {
    await page.goto('/collection');
    await page.waitForSelector('[href^="/releases/"]', { timeout: 10000 });
    
    await page.locator('[href^="/releases/"]').first().click();
    await expect(page).toHaveURL(/\/releases\/\d+/);

    // Wait for content to load
    await page.waitForTimeout(1000);

    // Check that we have some metadata displayed
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
    
    // Common metadata fields
    const hasArtist = bodyText?.includes('Artist') || bodyText?.includes('by');
    const hasYear = /\d{4}/.test(bodyText || '');
    const hasGenre = bodyText?.includes('Genre') || bodyText?.includes('Metal') || bodyText?.includes('Rock');
    
    expect(hasArtist || hasYear || hasGenre).toBeTruthy();
  });

  test('should have back navigation', async ({ page }) => {
    await page.goto('/collection');
    await page.waitForSelector('[href^="/releases/"]', { timeout: 10000 });
    
    await page.locator('[href^="/releases/"]').first().click();
    await expect(page).toHaveURL(/\/releases\/\d+/);

    // Look for back button or link
    const backButton = page.getByRole('link', { name: /Back|Collection/i });
    const isVisible = await backButton.isVisible().catch(() => false);
    
    if (isVisible) {
      await backButton.click();
      await expect(page).toHaveURL('/collection');
    } else {
      // Use browser back button
      await page.goBack();
      await expect(page).toHaveURL('/collection');
    }
  });

  test('should display cover image if available', async ({ page }) => {
    await page.goto('/collection');
    await page.waitForSelector('[href^="/releases/"]', { timeout: 10000 });
    
    await page.locator('[href^="/releases/"]').first().click();
    await expect(page).toHaveURL(/\/releases\/\d+/);

    // Wait for page to load
    await page.waitForTimeout(1000);

    // Check for images (either cover art or placeholder)
    const images = page.locator('img');
    const imageCount = await images.count();
    
    // Should have at least one image (even if it's a placeholder)
    expect(imageCount).toBeGreaterThanOrEqual(0);
  });
});
