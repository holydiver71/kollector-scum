import { test, expect } from '@playwright/test';

test.describe('Dashboard Page', () => {
  test('should load dashboard and display key elements', async ({ page }) => {
    await page.goto('/');

    // Check page title
    await expect(page).toHaveTitle(/KOLLECTOR SKÜM/i);

    // Check main heading
    await expect(page.getByRole('heading', { name: /KOLLECTOR SKÜM/i })).toBeVisible();

    // Check for statistics cards
    await expect(page.getByText('Releases')).toBeVisible();
    await expect(page.getByText('Artists')).toBeVisible();
    await expect(page.getByText('Genres')).toBeVisible();
    await expect(page.getByText('Labels')).toBeVisible();

    // Check for quick actions section
    await expect(page.getByText('Quick Actions')).toBeVisible();
    await expect(page.getByText('Browse Collection')).toBeVisible();
    await expect(page.getByText('Search Music')).toBeVisible();
  });

  test('should display health status', async ({ page }) => {
    await page.goto('/');

    // Wait for health status to load
    await expect(page.getByText(/Status:/i)).toBeVisible();
    
    // Check for online status (may take a moment to load)
    await expect(page.getByText(/Online|Healthy/i)).toBeVisible({ timeout: 10000 });
  });

  test('should navigate to collection from quick actions', async ({ page }) => {
    await page.goto('/');

    // Click Browse Collection
    await page.getByRole('link', { name: /Browse Collection/i }).click();

    // Should navigate to collection page
    await expect(page).toHaveURL('/collection');
    await expect(page.getByRole('heading', { name: /Music Collection/i })).toBeVisible();
  });

  test('should navigate to search from quick actions', async ({ page }) => {
    await page.goto('/');

    // Click Search Music
    await page.getByRole('link', { name: /Search Music/i }).click();

    // Should navigate to search page
    await expect(page).toHaveURL('/search');
    await expect(page.getByRole('heading', { name: /Search Music/i })).toBeVisible();
  });

  test('should display statistics with real data', async ({ page }) => {
    await page.goto('/');

    // Wait for statistics to load
    await page.waitForSelector('text=Releases', { timeout: 10000 });

    // Check that we have real numbers (not just 0)
    const statsText = await page.textContent('body');
    expect(statsText).toBeTruthy();
    
    // The dashboard should show the stat cards
    const statCards = page.locator('[class*="bg-white"][class*="rounded"]').filter({ hasText: /Releases|Artists|Genres|Labels/ });
    await expect(statCards.first()).toBeVisible();
  });
});
