import re

with open('frontend/app/components/MusicReleaseList.tsx', 'r') as f:
    content = f.read()

# Replace the Results Header block
start_str = "      {/* Results Header - hidden during initial load */}"
end_str = "      {/* Release Cards - Grid matching mock-up */}"
start_idx = content.find(start_str)
end_idx = content.find(end_str)

new_header = """      {/* Results Header */}
      {!loading && (
        <div className="space-y-6 mb-6">
          <div className="flex gap-3 flex-wrap items-center">
            <div className="flex-1 min-w-[200px] relative">
              <input 
                type="text" 
                placeholder="Search releases, artists, albums..." 
                className="w-full bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-white placeholder-gray-600 text-sm focus:outline-none focus:border-[#8B5CF6]"
                value={searchParams?.get('search') || ''}
                onChange={(e) => {
                  try {
                    const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                    if (e.target.value) params.set('search', e.target.value);
                    else params.delete('search');
                    const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                    router.replace(newUrl, { scroll: false });
                  } catch {}
                }}
              />
            </div>
            
            <select 
              className="bg-[#13131F] border border-[#1C1C28] rounded-xl px-4 py-3 text-gray-300 text-sm focus:outline-none focus:border-[#8B5CF6] cursor-pointer"
              value={`${filters.sortBy || 'DateAdded'}_${filters.sortOrder || 'desc'}`}
              onChange={(e) => {
                const [by, order] = e.target.value.split('_');
                applySortChange({ sortBy: by, sortOrder: order });
              }}
            >
              <option value="DateAdded_desc">Sort: Date Added</option>
              <option value="Title_asc">Title (A-Z)</option>
              <option value="Title_desc">Title (Z-A)</option>
              <option value="Artist_asc">Artist (A-Z)</option>
              <option value="Artist_desc">Artist (Z-A)</option>
              <option value="Year_desc">Year (Newest)</option>
              <option value="Year_asc">Year (Oldest)</option>
            </select>

            <button
              onClick={() => {
                try {
                  const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                  const currentlyOpen = params.get('showAdvanced') === 'true';
                  if (currentlyOpen) params.delete('showAdvanced');
                  else params.set('showAdvanced', 'true');
                  const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                  router.replace(newUrl, { scroll: false });
                } catch {}
              }}
              className={`px-5 rounded-xl text-sm font-medium flex items-center gap-2 transition-all h-[46px] ${
                searchParams?.get('showAdvanced') === 'true'
                  ? "bg-[#8B5CF6] text-white shadow-lg shadow-[#8B5CF6]/25"
                  : "bg-[#13131F] border border-[#1C1C28] text-gray-300 hover:text-white hover:border-[#2E2E3E]"
              }`}
            >
              <svg width="16" height="16" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" /></svg>
              Filters
              <svg width="12" height="12" viewBox="0 0 12 12" fill="currentColor" className={`transition-transform duration-200 ${searchParams?.get('showAdvanced') === 'true' ? "rotate-180" : ""}`}><path d="M2.5 4.5l3.5 3.5 3.5-3.5" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round" /></svg>
            </button>
          </div>
          
          {searchParams?.get('showAdvanced') === 'true' && (
            <div className="w-full mt-0">
              <SearchAndFilter
                onFiltersChange={(newFilters) => {
                  try {
                    const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                    Object.entries(newFilters as Record<string, unknown>).forEach(([k, v]) => {
                      if (v !== undefined && v !== null && v !== '') params.set(k, String(v));
                      else params.delete(k);
                    });
                    const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                    router.replace(newUrl, { scroll: false });
                  } catch {}
                }}
                initialFilters={filters}
                enableUrlSync={false}
                showSearchInput={false}
                openAdvanced={true}
                compact={true}
                onAdvancedToggle={(open) => {
                  try {
                    const params = new URLSearchParams(searchParams ? searchParams.toString() : '');
                    if (open) params.set('showAdvanced', 'true'); else params.delete('showAdvanced');
                    const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
                    router.replace(newUrl, { scroll: false });
                  } catch {}
                }}
              />
            </div>
          )}
        </div>
      )}

"""

if start_idx != -1 and end_idx != -1:
    with open('frontend/app/components/MusicReleaseList.tsx', 'w') as f:
        f.write(content[:start_idx] + new_header + content[end_idx:])
    print("Patched MusicReleaseList.tsx")
else:
    print("Could not find start or end block")
