import { computeRatioCurrent, selectPhasePrompt } from "../DiscogsImportDialog";

describe("computeRatioCurrent", () => {
  it("returns total when isCompleted is true", () => {
    expect(computeRatioCurrent(50, 100, true, 0, 85)).toBe(100);
  });

  it("returns 0 before the phase activates (pct <= minPct)", () => {
    expect(computeRatioCurrent(85, 50, false, 85, 95)).toBe(0);
    expect(computeRatioCurrent(80, 50, false, 85, 95)).toBe(0);
  });

  it("returns total when pct reaches maxPct", () => {
    expect(computeRatioCurrent(95, 50, false, 85, 95)).toBe(50);
  });

  it("returns proportional value at 50% through the phase range", () => {
    // pct = 90 → midpoint of [85, 95] → ratio = 0.5 → current = round(0.5 * 40) = 20
    expect(computeRatioCurrent(90, 40, false, 85, 95)).toBe(20);
  });

  it("clamps to total when pct exceeds maxPct", () => {
    expect(computeRatioCurrent(99, 50, false, 85, 95)).toBe(50);
  });
});

describe("selectPhasePrompt", () => {
  const prompts = ["Prompt A", "Prompt B", "Prompt C"];

  it("returns completedMsg when isComplete is true", () => {
    expect(selectPhasePrompt(true, 99, 85, 0, prompts, "Done!", "Waiting...")).toBe("Done!");
  });

  it("returns waitingMsg when pct is below activePct", () => {
    expect(selectPhasePrompt(false, 80, 85, 0, prompts, "Done!", "Waiting...")).toBe("Waiting...");
  });

  it("cycles through prompts based on completedItems", () => {
    expect(selectPhasePrompt(false, 90, 85, 0, prompts, "Done!", "Waiting...")).toBe("Prompt A");
    expect(selectPhasePrompt(false, 90, 85, 1, prompts, "Done!", "Waiting...")).toBe("Prompt B");
    expect(selectPhasePrompt(false, 90, 85, 2, prompts, "Done!", "Waiting...")).toBe("Prompt C");
    expect(selectPhasePrompt(false, 90, 85, 3, prompts, "Done!", "Waiting...")).toBe("Prompt A");
  });

  it("uses prompt at index 0 when pct equals activePct", () => {
    expect(selectPhasePrompt(false, 85, 85, 0, prompts, "Done!", "Waiting...")).toBe("Prompt A");
  });
});
