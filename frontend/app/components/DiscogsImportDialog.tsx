import { useEffect, useRef, useState, useCallback } from "react";
import { fetchJson } from "../lib/api";

export interface DiscogsImportDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

interface ImportResult {
  success: boolean;
  totalReleases: number;
  importedReleases: number;
  skippedReleases: number;
  failedReleases: number;
  errors: string[];
  duration: string;
}

/**
 * Dialog for importing collection from Discogs
 */
export function DiscogsImportDialog({
  isOpen,
  onClose,
  onSuccess,
}: DiscogsImportDialogProps) {
  const [username, setUsername] = useState("");
  const [isImporting, setIsImporting] = useState(false);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const dialogRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Focus username input when dialog opens
  useEffect(() => {
    if (isOpen && inputRef.current && !isImporting && !result) {
      inputRef.current.focus();
    }
  }, [isOpen, isImporting, result]);

  // Handle Escape key to close dialog
  // include `handleClose` in deps since it's referenced in the effect
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen && !isImporting) {
        handleClose();
      }
    };

    if (isOpen) {
      document.addEventListener("keydown", handleEscape);
      document.body.style.overflow = "hidden";
    }

    return () => {
      document.removeEventListener("keydown", handleEscape);
      document.body.style.overflow = "";
    };
  }, [isOpen, isImporting, handleClose]);

  const handleClose = useCallback(() => {
    if (!isImporting) {
      // If import was successful, trigger the success callback
      if (result?.success) {
        onSuccess();
      }
      setUsername("");
      setResult(null);
      setError(null);
      onClose();
    }
  }, [isImporting, result, onSuccess, onClose]);

  const handleImport = async () => {
    if (!username.trim()) {
      setError("Please enter a Discogs username");
      return;
    }

    setIsImporting(true);
    setError(null);
    setResult(null);

    try {
      const data = await fetchJson<ImportResult>("/api/import/discogs", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ username: username.trim() }),
        timeoutMs: 1800000, // 30 minute timeout for large collections
      });
      setResult(data);
      
      // Don't auto-close - let user review results and close manually
    } catch (err) {
      console.error("Error importing from Discogs:", err);
      // Prefer robust runtime checks rather than `any`.
      const e = err as unknown;
      if (err instanceof Error && err.name === 'AbortError') {
        setError('Import timed out. Please try again or contact support.');
      } else if (e && typeof e === 'object') {
        const errObj = e as Record<string, unknown>;
        if (typeof errObj.message === 'string') setError(errObj.message);
        else setError('Failed to import from Discogs. Please try again.');
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Failed to import from Discogs. Please try again.');
      }
    } finally {
      setIsImporting(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !isImporting && !result) {
      handleImport();
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
      onClick={(e) => {
        if (e.target === e.currentTarget && !isImporting) {
          handleClose();
        }
      }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="import-dialog-title"
    >
      <div
        ref={dialogRef}
        className="bg-gradient-to-br from-red-900 via-red-950 to-black rounded-lg border border-white/10 shadow-xl max-w-md w-full p-6 transform transition-all"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Dialog Header */}
        <div className="mb-4">
          <h2
            id="import-dialog-title"
            className="text-xl font-semibold text-white"
          >
            Import from Discogs
          </h2>
          <p className="text-sm text-gray-400 mt-1">
            Enter your Discogs username to import your collection
          </p>
          {!isImporting && !result && (
            <p className="text-xs text-gray-500 mt-2">
              Note: Large collections may take 10-20 minutes to import
            </p>
          )}
        </div>

        {/* Dialog Content */}
        <div className="mb-6">
          {!result && !isImporting && (
            <>
              <label
                htmlFor="discogs-username"
                className="block text-sm font-medium text-gray-300 mb-2"
              >
                Discogs Username
              </label>
              <input
                ref={inputRef}
                id="discogs-username"
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                onKeyPress={handleKeyPress}
                className="w-full px-3 py-2 bg-black/30 border border-white/20 rounded-md text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-transparent"
                placeholder="your_username"
                disabled={isImporting}
              />
              {error && (
                <p className="text-red-400 text-sm mt-2" role="alert">
                  {error}
                </p>
              )}
            </>
          )}

          {isImporting && (
            <div className="flex flex-col items-center justify-center py-8">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white mb-4"></div>
              <p className="text-gray-300 text-center">
                Importing your collection...
                <br />
                <span className="text-sm text-gray-400">
                  This may take several minutes for large collections.
                  <br />
                  Please do not close this dialog.
                </span>
              </p>
            </div>
          )}

          {result && (
            <div className="space-y-4">
              {result.success ? (
                <div className="bg-green-900/30 border border-green-500/50 rounded-md p-4">
                  <h3 className="text-green-400 font-semibold mb-2">
                    Import Successful!
                  </h3>
                  <div className="text-sm text-gray-300 space-y-1">
                    <p>Total releases: {result.totalReleases}</p>
                    <p>Imported: {result.importedReleases}</p>
                    <p>Skipped (already exist): {result.skippedReleases}</p>
                    {result.failedReleases > 0 && (
                      <p className="text-yellow-400">
                        Failed: {result.failedReleases}
                      </p>
                    )}
                    <p className="text-gray-400 text-xs mt-2">
                      Duration: {result.duration}
                    </p>
                  </div>
                </div>
              ) : (
                <div className="bg-red-900/30 border border-red-500/50 rounded-md p-4">
                  <h3 className="text-red-400 font-semibold mb-2">
                    Import Failed
                  </h3>
                  <p className="text-sm text-gray-300">
                    {result.errors.length > 0
                      ? result.errors[0]
                      : "An unknown error occurred"}
                  </p>
                </div>
              )}

              {result.errors.length > 1 && (
                <details className="text-sm">
                  <summary className="text-gray-400 cursor-pointer hover:text-gray-300">
                    View all errors ({result.errors.length})
                  </summary>
                  <div className="mt-2 space-y-1 max-h-40 overflow-y-auto">
                    {result.errors.slice(0, 10).map((err, idx) => (
                      <p key={idx} className="text-gray-400 text-xs">
                        • {err}
                      </p>
                    ))}
                    {result.errors.length > 10 && (
                      <p className="text-gray-500 text-xs italic">
                        ... and {result.errors.length - 10} more errors
                      </p>
                    )}
                  </div>
                </details>
              )}
            </div>
          )}
        </div>

        {/* Dialog Actions */}
        <div className="flex gap-3 justify-end">
          {!isImporting && (
            <button
              type="button"
              onClick={handleClose}
              className="px-4 py-2 text-sm font-medium text-white bg-white/10 border border-white/20 rounded-md hover:bg-white/20 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
            >
              {result ? "Close" : "Cancel"}
            </button>
          )}
          {!result && !isImporting && (
            <button
              type="button"
              onClick={handleImport}
              disabled={!username.trim()}
              className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed shadow-lg shadow-red-900/50"
            >
              Import Collection
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
