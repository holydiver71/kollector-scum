#!/bin/bash
# SWE-smith evaluation harness for kollector-scum
#
# Usage:
#   REPO_DIR=/testbed TEST_CMD="dotnet test backend/KollectorScum.Tests" ./eval.sh
#   PATCH_FILE=/path/to/bug.patch ./eval.sh
#
# Exit codes:
#   0  — tests FAILED  (bug confirmed / patch breaks tests)
#   1  — tests PASSED  (no failure detected)
#
# Note: SWE-smith convention inverts the usual meaning:
#   "success" for task VALIDATION = tests fail (bug is real).
#   "success" for agent EVALUATION = tests pass (fix is correct).
#   The harness always exits with the raw test exit code so the
#   caller can interpret it according to its own semantics.

set -eo pipefail

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------
REPO_DIR="${REPO_DIR:-/testbed}"
TEST_CMD="${TEST_CMD:-}"
PATCH_FILE="${PATCH_FILE:-}"

# ---------------------------------------------------------------------------
# Step into the repo
# ---------------------------------------------------------------------------
cd "$REPO_DIR"

echo "=== EVAL HARNESS: kollector-scum ==="
echo "REPO_DIR : $REPO_DIR"
echo "TEST_CMD : ${TEST_CMD:-(auto-detect)}"
echo "PATCH_FILE: ${PATCH_FILE:-(none)}"
echo ""

# ---------------------------------------------------------------------------
# Apply patch (if provided)
# ---------------------------------------------------------------------------
if [[ -n "$PATCH_FILE" ]]; then
    echo "--- Applying patch: $PATCH_FILE ---"
    git apply --whitespace=fix "$PATCH_FILE"
    echo "Patch applied successfully."
    echo ""
fi

# ---------------------------------------------------------------------------
# Detect test type from TEST_CMD
# ---------------------------------------------------------------------------
if [[ "$TEST_CMD" == *"npm"* ]]; then
    TEST_TYPE="frontend"
elif [[ "$TEST_CMD" == *"dotnet"* ]]; then
    TEST_TYPE="backend"
else
    # Default: run backend tests
    TEST_TYPE="backend"
fi

# ---------------------------------------------------------------------------
# Build effective command
# ---------------------------------------------------------------------------
if [[ -n "$TEST_CMD" ]]; then
    EFFECTIVE_CMD="$TEST_CMD"
else
    if [[ "$TEST_TYPE" == "frontend" ]]; then
        EFFECTIVE_CMD="npm --prefix frontend test -- --watchAll=false --passWithNoTests"
    else
        EFFECTIVE_CMD="dotnet test backend/KollectorScum.Tests"
    fi
fi

echo "--- Running $TEST_TYPE tests ---"
echo "CMD: $EFFECTIVE_CMD"
echo ""

# ---------------------------------------------------------------------------
# Run the tests — capture exit code without letting set -e abort us
# ---------------------------------------------------------------------------
set +e
eval "$EFFECTIVE_CMD"
TEST_EXIT_CODE=$?
set -e

# ---------------------------------------------------------------------------
# Determine PASS / FAIL
# ---------------------------------------------------------------------------
if [[ $TEST_EXIT_CODE -eq 0 ]]; then
    STATUS="PASS"
else
    STATUS="FAIL"
fi

# ---------------------------------------------------------------------------
# Structured summary
# ---------------------------------------------------------------------------
echo ""
echo "=== EVAL RESULT ==="
echo "TEST_CMD : $EFFECTIVE_CMD"
echo "EXIT_CODE: $TEST_EXIT_CODE"
echo "STATUS   : $STATUS"
echo "=================="

exit $TEST_EXIT_CODE
