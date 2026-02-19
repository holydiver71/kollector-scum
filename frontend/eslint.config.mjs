import { dirname } from "path";
import { fileURLToPath } from "url";
import pkg from "@eslint/eslintrc";
const { FlatCompat } = pkg;

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const compat = new FlatCompat({
  baseDirectory: __dirname,
});

const eslintConfig = [
  ...compat.extends("next/core-web-vitals", "next/typescript"),
  // Keep strict typing but allow `any` as a warning at the project level. Tests
  // continue to explicitly allow `any` and other relaxed rules.
  {
    rules: {
      '@typescript-eslint/no-explicit-any': 'warn',
    },
  },
  {
    ignores: [
      "node_modules/**",
      ".next/**",
      "out/**",
      "build/**",
      "next-env.d.ts",
    ],
  },
  // Test files commonly use `any` in quick mocks and sometimes use `require()` in
  // snapshot/test helpers. Relax linting for test files so these don't break a
  // production build and preserve stricter rules for app source code.
  {
    files: ["**/*.test.ts", "**/*.test.tsx", "**/*.spec.ts", "**/*.spec.tsx"],
    rules: {
      // Tests often use `any` for mock convenience — allow it in tests
      '@typescript-eslint/no-explicit-any': 'off',
      // Some tests use `require()` for dynamic module loading in snapshots
      '@typescript-eslint/no-require-imports': 'off',
      'no-restricted-syntax': 'off',
      // Tests may define components inline without display names — don't fail the build
      'react/display-name': 'off',
    },
  },
];

export default eslintConfig;
