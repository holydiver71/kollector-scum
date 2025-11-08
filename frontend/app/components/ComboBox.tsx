"use client";

import { useState, useRef, useEffect } from "react";

export interface ComboBoxItem {
  id: number;
  name: string;
}

interface ComboBoxProps {
  label: string;
  items: ComboBoxItem[];
  value: number[] | number | null; // Selected IDs
  newValues?: string[]; // New text values (not yet in database)
  onChange: (selectedIds: number[], newValues: string[]) => void;
  multiple?: boolean;
  required?: boolean;
  placeholder?: string;
  helpText?: string;
  error?: string;
  disabled?: boolean;
  allowCreate?: boolean; // Enable typing new values
}

export default function ComboBox({
  label,
  items,
  value,
  newValues = [],
  onChange,
  multiple = false,
  required = false,
  placeholder = "Select or type to add new...",
  helpText,
  error,
  disabled = false,
  allowCreate = true,
}: ComboBoxProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [highlightedIndex, setHighlightedIndex] = useState(0);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Normalize value to array
  const selectedIds = Array.isArray(value) ? value : value ? [value] : [];

  // Get selected items
  const selectedItems = items.filter((item) => selectedIds.includes(item.id));

  // Filter items based on search
  const filteredItems = items.filter((item) =>
    item.name.toLowerCase().includes(searchTerm.toLowerCase())
  );

  // Check if search term matches a new value
  const isNewValue =
    allowCreate &&
    searchTerm.trim() &&
    !filteredItems.some((item) => item.name.toLowerCase() === searchTerm.toLowerCase()) &&
    !newValues.some((val) => val.toLowerCase() === searchTerm.toLowerCase());

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setSearchTerm("");
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSelect = (itemId: number) => {
    if (multiple) {
      const newSelection = selectedIds.includes(itemId)
        ? selectedIds.filter((id) => id !== itemId)
        : [...selectedIds, itemId];
      onChange(newSelection, newValues);
    } else {
      onChange([itemId], newValues);
      setIsOpen(false);
      setSearchTerm("");
    }
  };

  const handleAddNew = () => {
    if (isNewValue && searchTerm.trim()) {
      const trimmedValue = searchTerm.trim();
      onChange(selectedIds, [...newValues, trimmedValue]);
      setSearchTerm("");
      if (!multiple) {
        setIsOpen(false);
      }
    }
  };

  const handleRemoveNew = (valueToRemove: string) => {
    onChange(
      selectedIds,
      newValues.filter((v) => v !== valueToRemove)
    );
  };

  const handleRemoveExisting = (idToRemove: number) => {
    onChange(
      selectedIds.filter((id) => id !== idToRemove),
      newValues
    );
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!isOpen && (e.key === "ArrowDown" || e.key === "Enter")) {
      setIsOpen(true);
      return;
    }

    if (!isOpen) return;

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        setHighlightedIndex((prev) =>
          prev < filteredItems.length - 1 + (isNewValue ? 1 : 0) ? prev + 1 : 0
        );
        break;
      case "ArrowUp":
        e.preventDefault();
        setHighlightedIndex((prev) =>
          prev > 0 ? prev - 1 : filteredItems.length - 1 + (isNewValue ? 1 : 0)
        );
        break;
      case "Enter":
        e.preventDefault();
        if (highlightedIndex < filteredItems.length) {
          handleSelect(filteredItems[highlightedIndex].id);
        } else if (isNewValue) {
          handleAddNew();
        }
        break;
      case "Escape":
        setIsOpen(false);
        setSearchTerm("");
        break;
    }
  };

  return (
    <div ref={containerRef} className="relative">
      <label htmlFor={label} className="block text-sm font-medium text-gray-700 mb-1">
        {label} {required && <span className="text-red-500">*</span>}
      </label>

      {/* Selected items display */}
      <div
        className={`min-h-[42px] w-full px-3 py-2 border rounded-md focus-within:ring-2 focus-within:ring-blue-500 focus-within:border-blue-500 ${
          error ? "border-red-500" : "border-gray-300"
        } ${disabled ? "bg-gray-50" : "bg-white"}`}
      >
        <div className="flex flex-wrap gap-2 mb-2">
          {/* Existing selected items */}
          {selectedItems.map((item) => (
            <span
              key={item.id}
              className="inline-flex items-center px-2 py-1 rounded-md text-sm bg-blue-100 text-blue-800"
            >
              {item.name}
              {!disabled && (
                <button
                  type="button"
                  onClick={() => handleRemoveExisting(item.id)}
                  className="ml-1 text-blue-600 hover:text-blue-800 focus:outline-none"
                  aria-label={`Remove ${item.name}`}
                >
                  ×
                </button>
              )}
            </span>
          ))}

          {/* New values (not yet in database) */}
          {newValues.map((val) => (
            <span
              key={val}
              className="inline-flex items-center px-2 py-1 rounded-md text-sm bg-green-100 text-green-800 border border-green-300"
            >
              <span className="text-xs mr-1">✨</span>
              {val}
              {!disabled && (
                <button
                  type="button"
                  onClick={() => handleRemoveNew(val)}
                  className="ml-1 text-green-600 hover:text-green-800 focus:outline-none"
                  aria-label={`Remove ${val}`}
                >
                  ×
                </button>
              )}
            </span>
          ))}
        </div>

        {/* Input field */}
        <input
          ref={inputRef}
          type="text"
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            if (!isOpen) setIsOpen(true);
            setHighlightedIndex(0);
          }}
          onFocus={() => setIsOpen(true)}
          onKeyDown={handleKeyDown}
          placeholder={selectedIds.length === 0 && newValues.length === 0 ? placeholder : ""}
          disabled={disabled || (!multiple && selectedIds.length > 0 && newValues.length === 0)}
          className="w-full outline-none bg-transparent text-sm"
        />
      </div>

      {/* Dropdown list */}
      {isOpen && !disabled && (
        <div className="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-auto">
          {filteredItems.length === 0 && !isNewValue && (
            <div className="px-3 py-2 text-sm text-gray-500">No results found</div>
          )}

          {filteredItems.map((item, index) => {
            const isSelected = selectedIds.includes(item.id);
            const isHighlighted = index === highlightedIndex;

            return (
              <div
                key={item.id}
                onClick={() => handleSelect(item.id)}
                className={`px-3 py-2 cursor-pointer text-sm ${
                  isHighlighted ? "bg-blue-50" : ""
                } ${isSelected ? "bg-blue-100 font-medium" : "hover:bg-gray-50"}`}
              >
                <div className="flex items-center justify-between">
                  <span>{item.name}</span>
                  {isSelected && <span className="text-blue-600">✓</span>}
                </div>
              </div>
            );
          })}

          {/* Add new option */}
          {isNewValue && (
            <div
              onClick={handleAddNew}
              className={`px-3 py-2 cursor-pointer text-sm border-t ${
                highlightedIndex === filteredItems.length ? "bg-green-50" : "hover:bg-gray-50"
              }`}
            >
              <div className="flex items-center">
                <span className="text-green-600 mr-2">✨</span>
                <span className="text-green-700 font-medium">
                  Create &quot;{searchTerm.trim()}&quot;
                </span>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Help text */}
      {helpText && <p className="mt-1 text-xs text-gray-500">{helpText}</p>}

      {/* Error message */}
      {error && <p className="mt-1 text-sm text-red-600">{error}</p>}

      {/* Info about new values */}
      {newValues.length > 0 && !error && (
        <p className="mt-1 text-xs text-green-600">
          {newValues.length} new {multiple ? "items" : "item"} will be created
        </p>
      )}
    </div>
  );
}
