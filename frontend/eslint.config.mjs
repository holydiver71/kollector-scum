import { dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { FlatCompat } from "@eslint/eslintrc";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const compat = new FlatCompat({
  baseDirectory: __dirname,
});

const config = [
  {
    ignores: [
      "node_modules/**",
      ".next/**",
      "coverage/**",
      "out/**",
      "build/**",
      "next-env.d.ts",
      ".eslintrc.cjs",
    ],
  },
  ...compat.config({
    parser: "@typescript-eslint/parser",
    parserOptions: {
      ecmaVersion: 2022,
      sourceType: "module",
      ecmaFeatures: { jsx: true },
    },
    plugins: ["@typescript-eslint", "react", "import"],
    extends: ["next/core-web-vitals", "next/typescript"],
    rules: {
      "@typescript-eslint/no-explicit-any": "warn",
    },
    settings: {
      react: {
        version: "detect",
      },
    },
    overrides: [
      {
        files: ["**/*.test.ts", "**/*.test.tsx", "**/*.spec.ts", "**/*.spec.tsx", "jest.setup.ts"],
        rules: {
          "@typescript-eslint/no-explicit-any": "off",
          "@typescript-eslint/no-require-imports": "off",
          "@typescript-eslint/no-var-requires": "off",
          "@typescript-eslint/ban-ts-comment": "off",
          "no-restricted-syntax": "off",
          "react/display-name": "off",
        },
      },
    ],
  }),
];

export default config;
