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
  preSelectedItems?: ComboBoxItem[]; // Items that should be displayed even if not in paginated items list
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
  preSelectedItems = [],
}: ComboBoxProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [highlightedIndex, setHighlightedIndex] = useState(0);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Normalize value to array
  const selectedIds = Array.isArray(value) ? value : value ? [value] : [];

  // Get selected items - merge items from both items list and preSelectedItems
  // This ensures that pre-selected items (e.g., from edit mode) are displayed
  // even if they're not in the current paginated items list
  const selectedItems = selectedIds
    .map(id => items.find(item => item.id === id) ?? preSelectedItems.find(item => item.id === id))
    .filter((item): item is ComboBoxItem => item !== undefined);

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

  // Position the fixed dropdown relative to the input container
  const updateDropdownPosition = () => {
    if (containerRef.current) {
      const rect = containerRef.current.getBoundingClientRect();
      setDropdownStyle({
        position: "fixed",
        top: rect.bottom + 4,
        left: rect.left,
        width: rect.width,
      });
    }
  };

  useEffect(() => {
    if (!isOpen) return;
    updateDropdownPosition();
    window.addEventListener("scroll", updateDropdownPosition, true);
    window.addEventListener("resize", updateDropdownPosition);
    return () => {
      window.removeEventListener("scroll", updateDropdownPosition, true);
      window.removeEventListener("resize", updateDropdownPosition);
    };
  }, [isOpen]);

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
      <label htmlFor={label} className="block text-sm font-medium text-[var(--theme-card-text)] mb-1">
        {label} {required && <span className="text-[var(--theme-error-text)]">*</span>}
      </label>

      {/* Selected items display */}
      <div
        className={`min-h-[42px] w-full px-3 py-2 border rounded-md text-[var(--theme-card-text)] focus-within:ring-2 focus-within:ring-[var(--theme-accent)] focus-within:border-[var(--theme-accent)] ${
          error ? "border-[var(--theme-error-border)]" : "border-[var(--theme-card-border)]"
        } ${disabled ? "bg-[var(--theme-sidebar-hover)]" : "bg-[var(--theme-card-bg)]"}`}
      >
        <div className="flex flex-wrap gap-2 mb-2">
          {/* Existing selected items */}
          {selectedItems.map((item) => (
            <span
              key={item.id}
              className="inline-flex items-center px-2 py-1 rounded-md text-sm bg-[var(--theme-sidebar-hover)] text-[var(--theme-card-text)] border border-[var(--theme-accent)]"
            >
              {item.name}
              {!disabled && (
                <button
                  type="button"
                  onClick={() => handleRemoveExisting(item.id)}
                  className="ml-1 text-[var(--theme-accent)] hover:text-[var(--theme-accent-hover)] focus:outline-none"
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
              className="inline-flex items-center px-2 py-1 rounded-md text-sm bg-[var(--theme-success-bg)] text-[var(--theme-success-text)] border border-[var(--theme-success-border)]"
            >
              <span className="text-xs mr-1">✨</span>
              {val}
              {!disabled && (
                <button
                  type="button"
                  onClick={() => handleRemoveNew(val)}
                  className="ml-1 text-[var(--theme-success-text)] hover:opacity-80 focus:outline-none"
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
          className="w-full outline-none bg-transparent text-sm text-[var(--theme-card-text)] placeholder:text-[var(--theme-muted-text)]"
        />
      </div>

      {/* Dropdown list */}
      {isOpen && !disabled && (
        <div
          className="z-50 bg-[var(--theme-card-bg)] border border-[var(--theme-card-border)] rounded-md shadow-lg max-h-60 overflow-y-auto"
          style={dropdownStyle}
        >
          {filteredItems.length === 0 && !isNewValue && (
            <div className="px-3 py-2 text-sm text-[var(--theme-muted-text)]">No results found</div>
          )}

          {filteredItems.map((item, index) => {
            const isSelected = selectedIds.includes(item.id);
            const isHighlighted = index === highlightedIndex;

            return (
              <div
                key={item.id}
                onClick={() => handleSelect(item.id)}
                className={`px-3 py-2 cursor-pointer text-sm text-[var(--theme-card-text)] ${
                  isHighlighted ? "bg-[var(--theme-sidebar-hover)]" : ""
                } ${isSelected ? "bg-[var(--theme-sidebar-hover)] font-medium" : "hover:bg-[var(--theme-sidebar-hover)]"}`}
              >
                <div className="flex items-center justify-between">
                  <span>{item.name}</span>
                  {isSelected && <span className="text-[var(--theme-accent)]">✓</span>}
                </div>
              </div>
            );
          })}

          {/* Add new option */}
          {isNewValue && (
            <div
              onClick={handleAddNew}
              className={`px-3 py-2 cursor-pointer text-sm border-t border-[var(--theme-card-border)] ${
                highlightedIndex === filteredItems.length ? "bg-[var(--theme-success-bg)]" : "hover:bg-[var(--theme-sidebar-hover)]"
              }`}
            >
              <div className="flex items-center">
                <span className="text-[var(--theme-success-text)] mr-2">✨</span>
                <span className="text-[var(--theme-success-text)] font-medium">
                  Create &quot;{searchTerm.trim()}&quot;
                </span>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Help text */}
      {helpText && <p className="mt-1 text-xs text-[var(--theme-muted-text)]">{helpText}</p>}

      {/* Error message */}
      {error && <p className="mt-1 text-sm text-[var(--theme-error-text)]">{error}</p>}

      {/* Info about new values */}
      {newValues.length > 0 && !error && (
        <p className="mt-1 text-xs text-[var(--theme-success-text)]">
          {newValues.length} new {multiple ? "items" : "item"} will be created
        </p>
      )}
    </div>
  );
}
