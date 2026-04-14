"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import type { WizardFormData, ValidationErrors, LookupItem } from "../types";
import type { ReleaseLookups } from "../useReleaseLookups";
import { WizardDateInput } from "../WizardDateInput";

/** Static list of supported currencies */
const CURRENCIES = [
  { value: "GBP", label: "GBP – British Pound" },
  { value: "USD", label: "USD – US Dollar" },
  { value: "EUR", label: "EUR – Euro" },
  { value: "JPY", label: "JPY – Japanese Yen" },
  { value: "CAD", label: "CAD – Canadian Dollar" },
  { value: "AUD", label: "AUD – Australian Dollar" },
  { value: "CHF", label: "CHF – Swiss Franc" },
  { value: "SEK", label: "SEK – Swedish Krona" },
  { value: "NOK", label: "NOK – Norwegian Krone" },
  { value: "DKK", label: "DKK – Danish Krone" },
  { value: "NZD", label: "NZD – New Zealand Dollar" },
  { value: "BRL", label: "BRL – Brazilian Real" },
  { value: "MXN", label: "MXN – Mexican Peso" },
  { value: "PLN", label: "PLN – Polish Zloty" },
  { value: "CZK", label: "CZK – Czech Koruna" },
];

/** Goldmine Grading Guide options */
const GOLDMINE_GRADES = [
  { value: "",    label: "— Not set —" },
  { value: "M",   label: "M – Mint" },
  { value: "NM",  label: "NM – Near Mint" },
  { value: "VG+", label: "VG+ – Very Good Plus" },
  { value: "VG",  label: "VG – Very Good" },
  { value: "G+",  label: "G+ – Good Plus" },
  { value: "G",   label: "G – Good" },
  { value: "F",   label: "F – Fair" },
  { value: "P",   label: "P – Poor" },
];

interface Props {
  /** Current form data */
  data: WizardFormData;
  /** Callback when any field changes */
  onChange: (updates: Partial<WizardFormData>) => void;
  /** Per-field validation errors */
  errors: ValidationErrors;
  /** Real lookup data from the API */
  lookups: ReleaseLookups;
}

/**
 * Panel 4 – Purchase Information (optional).
 * Collects store, purchase date, price, currency and notes.
 * The store field uses a custom autocomplete to select an existing store
 * (sets storeId + storeName) or enter a new store name (storeName only).
 */
export default function PurchaseInformationPanel({ data, onChange, errors, lookups }: Props) {
  const purchase = data.purchaseInfo;

  const [storeInput, setStoreInput] = useState(purchase.storeName ?? "");
  const [showStoreSuggestions, setShowStoreSuggestions] = useState(false);
  const storeInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setStoreInput(purchase.storeName ?? "");
  }, [purchase.storeName]);

  const normalizedStoreInput = storeInput.trim().toLowerCase();

  const filteredStores = useMemo<LookupItem[]>(() => {
    if (!normalizedStoreInput) {
      return lookups.stores.slice(0, 8);
    }

    return lookups.stores
      .filter((s) => s.name.toLowerCase().includes(normalizedStoreInput))
      .slice(0, 8);
  }, [lookups.stores, normalizedStoreInput]);

  const hasExactStoreMatch = lookups.stores.some(
    (s) => s.name.toLowerCase() === normalizedStoreInput
  );
  const showCreateStoreOption =
    storeInput.trim().length > 0 && !hasExactStoreMatch;

  const update = (patch: Partial<typeof data.purchaseInfo>) => {
    onChange({ purchaseInfo: { ...purchase, ...patch } });
  };

  const selectStore = (store: LookupItem) => {
    setStoreInput(store.name);
    setShowStoreSuggestions(false);
    update({ storeId: store.id, storeName: store.name });
  };

  const applyTypedStoreName = () => {
    const nextValue = storeInput.trim();
    setStoreInput(nextValue);
    setShowStoreSuggestions(false);
    update({ storeId: undefined, storeName: nextValue || undefined });
  };

  const handleStoreInputChange = (value: string) => {
    setStoreInput(value);
    setShowStoreSuggestions(true);

    const nextValue = value.trim();
    if (!nextValue) {
      update({ storeId: undefined, storeName: undefined });
      return;
    }

    const exact = lookups.stores.find(
      (s) => s.name.toLowerCase() === nextValue.toLowerCase()
    );
    update({ storeId: exact?.id, storeName: nextValue });
  };

  return (
    <div className="space-y-5">
      <p className="text-sm text-gray-500">
        All fields on this panel are optional. Fill in as much or as little as
        you like.
      </p>

      {/* Store + Date */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label
              htmlFor="wiz-store"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Store / Seller
            </label>
            <div className="relative">
              <input
                ref={storeInputRef}
                id="wiz-store"
                type="text"
                role="combobox"
                aria-autocomplete="list"
                aria-expanded={showStoreSuggestions}
                aria-controls="wiz-store-suggestions"
                value={storeInput}
                onChange={(e) => handleStoreInputChange(e.target.value)}
                onFocus={() => setShowStoreSuggestions(true)}
                onBlur={() => setTimeout(() => setShowStoreSuggestions(false), 150)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    if (filteredStores.length > 0) {
                      selectStore(filteredStores[0]);
                    } else {
                      applyTypedStoreName();
                    }
                  }
                  if (e.key === "Escape") setShowStoreSuggestions(false);
                }}
                placeholder="Select or type a store name…"
                className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 pr-12 text-white placeholder-gray-600 focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
              />
              <button
                type="button"
                aria-label={showStoreSuggestions ? "Hide saved stores" : "Browse saved stores"}
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => {
                  setShowStoreSuggestions((prev) => !prev);
                  storeInputRef.current?.focus();
                }}
                className="absolute inset-y-0 right-0 px-3 text-gray-400 hover:text-white transition-colors"
              >
                ▾
              </button>

              {showStoreSuggestions && (filteredStores.length > 0 || showCreateStoreOption) && (
                <div className="absolute z-20 w-full mt-1 bg-[#13131F] border border-[#1C1C28] rounded-lg shadow-xl overflow-hidden">
                  {filteredStores.length > 0 && (
                    <ul id="wiz-store-suggestions" role="listbox">
                      {filteredStores.map((s) => (
                        <li key={s.id}>
                          <button
                            type="button"
                            onMouseDown={(e) => {
                              e.preventDefault();
                              selectStore(s);
                            }}
                            className={`w-full text-left px-4 py-2.5 text-sm transition-colors ${
                              purchase.storeId === s.id
                                ? "bg-[#8B5CF6]/20 text-white"
                                : "text-gray-200 hover:bg-[#8B5CF6]/20 hover:text-white"
                            }`}
                          >
                            {s.name}
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}

                  {showCreateStoreOption && (
                    <button
                      type="button"
                      onMouseDown={(e) => {
                        e.preventDefault();
                        applyTypedStoreName();
                      }}
                      className="w-full text-left px-4 py-2.5 text-sm text-[#C4B5FD] border-t border-[#1C1C28] hover:bg-[#8B5CF6]/10 transition-colors"
                    >
                      Use “{storeInput.trim()}” as a new store
                    </button>
                  )}
                </div>
              )}
            </div>
            <p className="mt-2 text-xs text-gray-500">
              Pick a saved store or type a new one.
            </p>
          </div>

          <div>
            <label
              htmlFor="wiz-purchaseDate"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Purchase Date
            </label>
            <WizardDateInput
              id="wiz-purchaseDate"
              value={purchase.purchaseDate ?? ""}
              onChange={(iso) => update({ purchaseDate: iso || undefined })}
              label="Pick a purchase date"
            />
          </div>
        </div>
      </div>

      {/* Price + Currency */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label
              htmlFor="wiz-price"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Price
            </label>
            <input
              id="wiz-price"
              type="number"
              min={0}
              step={0.01}
              value={purchase.price ?? ""}
              onChange={(e) =>
                update({
                  price:
                    e.target.value === ""
                      ? undefined
                      : parseFloat(e.target.value),
                })
              }
              placeholder="0.00"
              className={`w-full bg-[#0F0F1A] border rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:ring-1 transition-colors ${
                errors.price
                  ? "border-red-500 focus:ring-red-500"
                  : "border-[#2A2A3C] focus:border-[#8B5CF6] focus:ring-[#8B5CF6]"
              }`}
            />
            {errors.price && (
              <p className="mt-1.5 text-sm text-red-400" role="alert">
                {errors.price}
              </p>
            )}
          </div>

          <div>
            <label
              htmlFor="wiz-currency"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Currency
            </label>
            <select
              id="wiz-currency"
              value={purchase.currency ?? "GBP"}
              onChange={(e) => update({ currency: e.target.value })}
              className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors appearance-none"
            >
              {CURRENCIES.map((c) => (
                <option key={c.value} value={c.value}>
                  {c.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Notes */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <label
          htmlFor="wiz-purchaseNotes"
          className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
        >
          Notes
        </label>
        <textarea
          id="wiz-purchaseNotes"
          rows={3}
          value={purchase.notes ?? ""}
          onChange={(e) => update({ notes: e.target.value })}
          placeholder="e.g. Original UK press in excellent condition"
          className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors resize-none"
        />
      </div>

      {/* Sleeve + Media Condition */}
      <div className="bg-[#0A0A12] rounded-xl p-4 border border-[#1C1C28]">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label
              htmlFor="wiz-sleeveCondition"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Sleeve Condition
            </label>
            <select
              id="wiz-sleeveCondition"
              value={purchase.sleeveCondition ?? ""}
              onChange={(e) => update({ sleeveCondition: e.target.value || undefined })}
              className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors appearance-none"
            >
              {GOLDMINE_GRADES.map((g) => (
                <option key={g.value} value={g.value}>
                  {g.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label
              htmlFor="wiz-mediaCondition"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Media Condition
            </label>
            <select
              id="wiz-mediaCondition"
              value={purchase.mediaCondition ?? ""}
              onChange={(e) => update({ mediaCondition: e.target.value || undefined })}
              className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors appearance-none"
            >
              {GOLDMINE_GRADES.map((g) => (
                <option key={g.value} value={g.value}>
                  {g.label}
                </option>
              ))}
            </select>
          </div>
        </div>
        <p className="mt-2 text-xs text-gray-500">
          Goldmine Grading Guide: M (Mint), NM (Near Mint), VG+, VG, G+, G, F (Fair), P (Poor).
        </p>
      </div>
    </div>
  );
}
