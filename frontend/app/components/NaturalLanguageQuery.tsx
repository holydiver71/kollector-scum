"use client";
import React, { useState } from 'react';
import { fetchJson, ApiError } from '../lib/api';

interface QueryResponse {
  question: string;
  query?: string;
  results?: Record<string, unknown>[];
  resultCount: number;
  answer: string;
  success: boolean;
  error?: string;
}

interface NaturalLanguageQueryProps {
  showSql?: boolean; // Whether to show the generated SQL (for debugging)
}

export function NaturalLanguageQuery({ showSql = false }: NaturalLanguageQueryProps) {
  const [question, setQuestion] = useState('');
  const [loading, setLoading] = useState(false);
  const [response, setResponse] = useState<QueryResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!question.trim()) return;

    setLoading(true);
    setError(null);
    setResponse(null);

    try {
      const result = await fetchJson<QueryResponse>('/api/query/ask', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ question: question.trim() }),
        timeoutMs: 30000, // 30 second timeout for LLM queries
      });
      setResponse(result);
      if (!result.success && result.error) {
        setError(result.error);
      }
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || 'Failed to process query');
    } finally {
      setLoading(false);
    }
  };

  const exampleQuestions = [
    "How many CDs do I have?",
    "Show me all vinyl releases from the 1980s",
    "What are my most recent additions?",
    "Which artists have the most releases?",
    "Show me all live recordings",
  ];

  return (
    <div className="w-full max-w-4xl mx-auto">
      {/* Query Form */}
      <form onSubmit={handleSubmit} className="mb-6">
        <div className="flex gap-2">
          <input
            type="text"
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            placeholder="Ask a question about your collection..."
            className="flex-1 px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none text-gray-900 placeholder-gray-500"
            disabled={loading}
          />
          <button
            type="submit"
            disabled={loading || !question.trim()}
            className="px-6 py-3 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
          >
            {loading ? (
              <span className="flex items-center gap-2">
                <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                Thinking...
              </span>
            ) : (
              'Ask'
            )}
          </button>
        </div>
      </form>

      {/* Example Questions */}
      {!response && !loading && (
        <div className="mb-6">
          <p className="text-sm text-gray-600 mb-2 font-medium">Try asking:</p>
          <div className="flex flex-wrap gap-2">
            {exampleQuestions.map((q, i) => (
              <button
                key={i}
                onClick={() => setQuestion(q)}
                className="px-3 py-1 text-sm bg-gray-100 text-gray-700 rounded-full hover:bg-gray-200 transition-colors"
              >
                {q}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Error Display */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
          <div className="flex items-start gap-3">
            <span className="text-xl">⚠️</span>
            <div>
              <h3 className="font-semibold text-red-800">Error</h3>
              <p className="text-red-700 text-sm">{error}</p>
            </div>
          </div>
        </div>
      )}

      {/* Response Display */}
      {response && response.success && (
        <div className="space-y-6">
          {/* Natural Language Answer */}
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
            <div className="flex items-start gap-3">
              <span className="text-2xl">💬</span>
              <div>
                <p className="text-gray-800 text-lg">{response.answer}</p>
                <p className="text-sm text-gray-500 mt-2">
                  Found {response.resultCount} result{response.resultCount !== 1 ? 's' : ''}
                </p>
              </div>
            </div>
          </div>

          {/* Generated SQL (if enabled) */}
          {showSql && response.query && (
            <details className="bg-gray-50 border border-gray-200 rounded-lg">
              <summary className="px-4 py-3 cursor-pointer font-medium text-gray-700 hover:bg-gray-100">
                View Generated SQL
              </summary>
              <pre className="px-4 py-3 text-sm text-gray-800 overflow-x-auto border-t border-gray-200 bg-gray-100">
                {response.query}
              </pre>
            </details>
          )}

          {/* Results Table */}
          {response.results && response.results.length > 0 && (
            <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
              <div className="px-4 py-3 bg-gray-50 border-b border-gray-200">
                <h3 className="font-semibold text-gray-800">Results</h3>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      {Object.keys(response.results[0]).map((key) => (
                        <th key={key} className="px-4 py-2 text-left font-semibold text-gray-700 border-b">
                          {key}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {response.results.slice(0, 50).map((row, i) => (
                      <tr key={i} className="border-b border-gray-100 hover:bg-gray-50">
                        {Object.values(row).map((value, j) => (
                          <td key={j} className="px-4 py-2 text-gray-800">
                            {value === null ? (
                              <span className="text-gray-400 italic">null</span>
                            ) : typeof value === 'object' ? (
                              JSON.stringify(value)
                            ) : (
                              String(value)
                            )}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {response.results.length > 50 && (
                <div className="px-4 py-2 bg-gray-50 border-t text-sm text-gray-500">
                  Showing first 50 of {response.results.length} results
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
