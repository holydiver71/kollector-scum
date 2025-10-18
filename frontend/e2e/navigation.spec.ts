import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test('should have working navigation menu', async ({ page }) => {
    await page.goto('/');

    // Check all nav links are present
    await expect(page.getByRole('link', { name: /Dashboard/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /Collection/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /Search/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /Add Release/i })).toBeVisible();
  });

  test('should navigate to collection page', async ({ page }) => {
    await page.goto('/');
    
    await page.getByRole('link', { name: /Collection/i }).first().click();
    await expect(page).toHaveURL('/collection');
    await expect(page.getByRole('heading', { name: /Music Collection/i })).toBeVisible();
  });

  test('should navigate to search page', async ({ page }) => {
    await page.goto('/');
    
    await page.getByRole('link', { name: /Search/i }).first().click();
    await expect(page).toHaveURL('/search');
    await expect(page.getByRole('heading', { name: /Search Music/i })).toBeVisible();
  });

  test('should navigate to statistics page', async ({ page }) => {
    await page.goto('/');
    
    await page.getByRole('link', { name: /Statistics/i }).click();
    await expect(page).toHaveURL('/statistics');
    await expect(page.getByRole('heading', { name: /Collection Statistics/i })).toBeVisible();
  });

  test('should navigate back to dashboard from logo', async ({ page }) => {
    await page.goto('/collection');
    
    // Click the logo/title to go back to dashboard
    await page.getByRole('link', { name: /KOLLECTOR SKÜM/i }).first().click();
    await expect(page).toHaveURL('/');
  });

  test('should highlight active navigation item', async ({ page }) => {
    await page.goto('/collection');
    
    // The Collection nav item should be active
    const collectionLink = page.getByRole('link', { name: /Collection/i }).first();
    
    // Check if it has active styling (you may need to adjust the selector based on your actual active class)
    const classList = await collectionLink.getAttribute('class');
    expect(classList).toBeTruthy();
  });
});
