"use client";

import { useState } from "react";
import type { WizardFormData, ValidationErrors, LookupItem } from "../types";
import type { ReleaseLookups } from "../useReleaseLookups";

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

  const filteredStores: LookupItem[] = storeInput.trim()
    ? lookups.stores
        .filter((s) => s.name.toLowerCase().includes(storeInput.toLowerCase()))
        .slice(0, 8)
    : [];

  const selectStore = (store: LookupItem) => {
    setStoreInput(store.name);
    setShowStoreSuggestions(false);
    update({ storeId: store.id, storeName: store.name });
  };

  const handleStoreInputChange = (value: string) => {
    setStoreInput(value);
    setShowStoreSuggestions(true);
    const exact = lookups.stores.find(
      (s) => s.name.toLowerCase() === value.toLowerCase()
    );
    update({ storeId: exact?.id, storeName: value });
  };

  const update = (patch: Partial<typeof data.purchaseInfo>) => {
    onChange({ purchaseInfo: { ...purchase, ...patch } });
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
                id="wiz-store"
                type="text"
                value={storeInput}
                onChange={(e) => handleStoreInputChange(e.target.value)}
                onFocus={() => setShowStoreSuggestions(true)}
                onBlur={() => setTimeout(() => setShowStoreSuggestions(false), 150)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    if (filteredStores.length > 0) selectStore(filteredStores[0]);
                  }
                  if (e.key === "Escape") setShowStoreSuggestions(false);
                }}
                placeholder="Search or type a store name…"
                className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white placeholder-gray-600 focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
              />

              {showStoreSuggestions && filteredStores.length > 0 && (
                <ul className="absolute z-20 w-full mt-1 bg-[#13131F] border border-[#1C1C28] rounded-lg shadow-xl overflow-hidden">
                  {filteredStores.map((s) => (
                    <li key={s.id}>
                      <button
                        type="button"
                        onMouseDown={() => selectStore(s)}
                        className="w-full text-left px-4 py-2.5 text-sm text-gray-200 hover:bg-[#8B5CF6]/20 hover:text-white transition-colors"
                      >
                        {s.name}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>

          <div>
            <label
              htmlFor="wiz-purchaseDate"
              className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
            >
              Purchase Date
            </label>
            <input
              id="wiz-purchaseDate"
              type="date"
              value={purchase.purchaseDate ?? ""}
              onChange={(e) => update({ purchaseDate: e.target.value })}
              className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors"
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
    </div>
  );
}
