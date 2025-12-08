import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
	testDir: 'e2e',
	timeout: 30_000,
	expect: {
		toHaveScreenshot: { maxDiffPixelRatio: 0.02 }
	},
	fullyParallel: true,
	forbidOnly: !!process.env.CI,
	retries: process.env.CI ? 1 : 0,
	workers: process.env.CI ? 1 : undefined,
	use: {
		baseURL: 'http://localhost:3000',
		headless: true,
		actionTimeout: 5_000,
		viewport: { width: 1280, height: 800 },
		trace: 'on-first-retry',
	},
	webServer: {
		command: 'npm run dev',
		port: 3000,
		reuseExistingServer: !process.env.CI,
	},
	projects: [
		{
			name: 'chromium',
			use: { ...devices['Desktop Chrome'] },
		},
	],
});

