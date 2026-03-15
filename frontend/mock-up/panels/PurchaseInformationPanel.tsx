"use client";

import type { MockFormData, ValidationErrors } from "../types";
import { STORES, CURRENCIES } from "../fixtures";

interface Props {
  data: MockFormData;
  onChange: (updates: Partial<MockFormData>) => void;
  errors: ValidationErrors;
}

/**
 * Panel 6 – Purchase Information (optional).
 * Collects store, purchase date, price, currency and notes.
 */
export default function PurchaseInformationPanel({ data, onChange, errors }: Props) {
  const purchase = data.purchaseInfo;

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
            htmlFor="mock-store"
            className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
          >
            Store / Seller
          </label>
          <select
            id="mock-store"
            value={purchase.storeName ?? ""}
            onChange={(e) => update({ storeName: e.target.value })}
            className="w-full bg-[#0F0F1A] border border-[#2A2A3C] rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#8B5CF6] focus:ring-1 focus:ring-[#8B5CF6] transition-colors appearance-none"
          >
            <option value="">Select a store…</option>
            {STORES.map((s) => (
              <option key={s.id} value={s.name}>
                {s.name}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label
            htmlFor="mock-purchaseDate"
            className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
          >
            Purchase Date
          </label>
          <input
            id="mock-purchaseDate"
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
            htmlFor="mock-price"
            className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
          >
            Price
          </label>
          <input
            id="mock-price"
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
            htmlFor="mock-currency"
            className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
          >
            Currency
          </label>
          <select
            id="mock-currency"
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
          htmlFor="mock-purchaseNotes"
          className="block text-xs font-semibold uppercase tracking-wider text-[#A78BFA]/70 mb-2"
        >
          Notes
        </label>
        <textarea
          id="mock-purchaseNotes"
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
