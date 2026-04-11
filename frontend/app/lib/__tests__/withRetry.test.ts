import { withRetry, defaultIsRetryable, httpOnlyIsRetryable } from "../withRetry";
import type { ApiError } from "../api";

function makeApiError(status?: number, retryAfter?: number): ApiError {
  const err = new Error("api error") as ApiError;
  err.status = status;
  err.retryAfter = retryAfter;
  return err;
}

describe("withRetry", () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.runAllTimers();
    jest.useRealTimers();
    jest.restoreAllMocks();
  });

  it("returns result on first success", async () => {
    const fn = jest.fn().mockResolvedValue("ok");
    await expect(withRetry(fn)).resolves.toBe("ok");
    expect(fn).toHaveBeenCalledTimes(1);
  });

  it("retries on network error (no status) and succeeds on 2nd attempt", async () => {
    const networkErr = new Error("network failure");
    const fn = jest
      .fn()
      .mockRejectedValueOnce(networkErr)
      .mockResolvedValueOnce("ok");

    const promise = withRetry(fn, { baseDelayMs: 100 });
    await jest.runAllTimersAsync();
    await expect(promise).resolves.toBe("ok");
    expect(fn).toHaveBeenCalledTimes(2);
  });

  it("retries on 429 and succeeds on 2nd attempt", async () => {
    const rateLimitErr = makeApiError(429);
    const fn = jest
      .fn()
      .mockRejectedValueOnce(rateLimitErr)
      .mockResolvedValueOnce("data");

    const promise = withRetry(fn, { baseDelayMs: 100 });
    await jest.runAllTimersAsync();
    await expect(promise).resolves.toBe("data");
    expect(fn).toHaveBeenCalledTimes(2);
  });

  it("retries on 5xx error", async () => {
    const serverErr = makeApiError(500);
    const fn = jest
      .fn()
      .mockRejectedValueOnce(serverErr)
      .mockResolvedValueOnce("result");

    const promise = withRetry(fn, { baseDelayMs: 100 });
    await jest.runAllTimersAsync();
    await expect(promise).resolves.toBe("result");
    expect(fn).toHaveBeenCalledTimes(2);
  });

  it("does NOT retry on 401 (non-retryable)", async () => {
    const authErr = makeApiError(401);
    const fn = jest.fn().mockRejectedValue(authErr);
    await expect(withRetry(fn)).rejects.toBe(authErr);
    expect(fn).toHaveBeenCalledTimes(1);
  });

  it("does NOT retry on 404 (non-retryable)", async () => {
    const notFoundErr = makeApiError(404);
    const fn = jest.fn().mockRejectedValue(notFoundErr);
    await expect(withRetry(fn)).rejects.toBe(notFoundErr);
    expect(fn).toHaveBeenCalledTimes(1);
  });

  it("exhausts all attempts and throws last error", async () => {
    const serverErr = makeApiError(503);
    const fn = jest.fn().mockRejectedValue(serverErr);

    // Attach the rejection handler BEFORE advancing timers to avoid
    // an unhandled-rejection warning from jest between the timer
    // advancement and the await on the next line.
    const resultPromise = withRetry(fn, { maxAttempts: 3, baseDelayMs: 100 });
    const rejectCheck = expect(resultPromise).rejects.toBe(serverErr);
    await jest.runAllTimersAsync();
    await rejectCheck;
    expect(fn).toHaveBeenCalledTimes(3);
  });

  it("accepts a custom isRetryable predicate", async () => {
    const neverRetry = () => false;
    const serverErr = makeApiError(500);
    const fn = jest.fn().mockRejectedValue(serverErr);

    await expect(withRetry(fn, { isRetryable: neverRetry })).rejects.toBe(serverErr);
    expect(fn).toHaveBeenCalledTimes(1);
  });

  it("respects Retry-After header on 429 (uses longer delay than baseDelayMs)", async () => {
    // Capture delays by intercepting the internal setTimeout via fake timers.
    const recordedDelays: number[] = [];
    const realSetTimeout = global.setTimeout;
    jest.spyOn(global, "setTimeout").mockImplementation(
      (handler: TimerHandler, delay?: number, ...args: unknown[]) => {
        recordedDelays.push(delay ?? 0);
        // Call the handler immediately (fake-timers-safe) so withRetry can progress.
        (handler as () => void)();
        return 0 as unknown as ReturnType<typeof setTimeout>;
      }
    );

    const rateLimitErr = makeApiError(429, 5); // Retry-After: 5 s → 5000 ms
    const fn = jest
      .fn()
      .mockRejectedValueOnce(rateLimitErr)
      .mockResolvedValueOnce("ok");

    await expect(withRetry(fn, { baseDelayMs: 100 })).resolves.toBe("ok");

    expect(recordedDelays[0]).toBe(5000);
  });

  it("uses exponential back-off when no Retry-After", async () => {
    const recordedDelays: number[] = [];
    jest.spyOn(global, "setTimeout").mockImplementation(
      (handler: TimerHandler, delay?: number) => {
        recordedDelays.push(delay ?? 0);
        (handler as () => void)();
        return 0 as unknown as ReturnType<typeof setTimeout>;
      }
    );

    const serverErr = makeApiError(503);
    const fn = jest
      .fn()
      .mockRejectedValueOnce(serverErr)
      .mockRejectedValueOnce(serverErr)
      .mockResolvedValueOnce("ok");

    await expect(withRetry(fn, { maxAttempts: 3, baseDelayMs: 500 })).resolves.toBe("ok");

    expect(recordedDelays[0]).toBe(500);  // baseDelayMs * 2^0
    expect(recordedDelays[1]).toBe(1000); // baseDelayMs * 2^1
  });
});

describe("defaultIsRetryable", () => {
  it("returns true for network errors (no status)", () => {
    expect(defaultIsRetryable(new Error("network"))).toBe(true);
  });

  it("returns true for 429", () => {
    expect(defaultIsRetryable(makeApiError(429))).toBe(true);
  });

  it("returns true for 500+", () => {
    expect(defaultIsRetryable(makeApiError(500))).toBe(true);
    expect(defaultIsRetryable(makeApiError(503))).toBe(true);
  });

  it("returns false for 4xx client errors", () => {
    expect(defaultIsRetryable(makeApiError(400))).toBe(false);
    expect(defaultIsRetryable(makeApiError(401))).toBe(false);
    expect(defaultIsRetryable(makeApiError(403))).toBe(false);
    expect(defaultIsRetryable(makeApiError(404))).toBe(false);
    expect(defaultIsRetryable(makeApiError(422))).toBe(false);
  });
});

describe("httpOnlyIsRetryable", () => {
  it("returns false for network errors (no status)", () => {
    expect(httpOnlyIsRetryable(new Error("network"))).toBe(false);
  });

  it("returns true for 429", () => {
    expect(httpOnlyIsRetryable(makeApiError(429))).toBe(true);
  });

  it("returns true for 500+", () => {
    expect(httpOnlyIsRetryable(makeApiError(500))).toBe(true);
  });

  it("returns false for 4xx", () => {
    expect(httpOnlyIsRetryable(makeApiError(401))).toBe(false);
    expect(httpOnlyIsRetryable(makeApiError(404))).toBe(false);
  });
});
