import type { ApiError } from "./api";

export interface WithRetryOptions {
  /** Maximum number of attempts (default: 3) */
  maxAttempts?: number;
  /** Base delay in ms for exponential back-off (default: 1000) */
  baseDelayMs?: number;
  /**
   * Determines whether an error warrants a retry.
   * Defaults to: network errors (no HTTP status), 429, and 5xx.
   */
  isRetryable?: (err: unknown) => boolean;
}

/** Default retry predicate: retries on network failures, rate-limiting, and server errors. */
export function defaultIsRetryable(err: unknown): boolean {
  const status = (err as ApiError)?.status;
  return !status || status === 429 || status >= 500;
}

/** Retry predicate that skips network errors (only 429 / 5xx). Used for lookup fetches. */
export function httpOnlyIsRetryable(err: unknown): boolean {
  const status = (err as ApiError)?.status;
  return !!status && (status === 429 || status >= 500);
}

/**
 * Runs `fn` with exponential back-off retry on transient failures.
 *
 * Respects the `Retry-After` header on 429 responses via `ApiError.retryAfter`.
 * Non-retryable errors (e.g. 401, 403, 422) are thrown immediately on the
 * first failure — they do not consume retry attempts.
 */
export async function withRetry<T>(
  fn: () => Promise<T>,
  options: WithRetryOptions = {}
): Promise<T> {
  const {
    maxAttempts = 3,
    baseDelayMs = 1000,
    isRetryable = defaultIsRetryable,
  } = options;

  let attempt = 0;
  let lastErr: unknown;

  while (attempt < maxAttempts) {
    attempt += 1;
    try {
      return await fn();
    } catch (err) {
      lastErr = err;
      if (!isRetryable(err) || attempt >= maxAttempts) break;
      const apiErr = err as ApiError;
      const delayMs =
        apiErr?.status === 429 && apiErr.retryAfter
          ? apiErr.retryAfter * 1000
          : baseDelayMs * Math.pow(2, attempt - 1);
      await new Promise<void>((r) => setTimeout(r, delayMs));
    }
  }

  throw lastErr;
}
