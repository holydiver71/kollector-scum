import re

with open('frontend/app/collection/page.tsx', 'r') as f:
    content = f.read()

# Replace "            {/* Active filters display (show currently applied filters as chips) */}" 
# all the way to "            {/* Sort Controls removed" 
start_str = "            {/* Active filters display (show currently applied filters as chips) */}"
end_str = "            {/* Sort Controls removed" 

start_idx = content.find(start_str)
end_idx = content.find(end_str)

if start_idx == -1 or end_idx == -1:
    print("Could not find the block")
    exit(1)

chips_content = content[start_idx:end_idx]

# We want to extract what's inside the {hasAppliedFilters && ( ... )}

pattern = r'\{hasAppliedFilters && \(\s*<div className="mb-6">\s*<div className="flex flex-wrap gap-2 sm:gap-3 items-center">(.*?)</div>\s*</div>\s*\)'

match = re.search(pattern, chips_content, flags=re.DOTALL)
if match:
    inner_chips = match.group(1)
else:
    print("Could not parse chips")
    exit(1)

active_filters_node = f"""            {{/* Active filters display (show currently applied filters as chips) */}}"""

# Now we construct the variable at the top of the function
# We should embed it directly as a prop to MusicReleaseList
replacement = f"""            {{/* Active filters are passed into MusicReleaseList */}}
"""

new_content = content[:start_idx] + replacement + content[end_idx:]

music_list_start = "<MusicReleaseList"
ml_idx = new_content.find(music_list_start)
if ml_idx != -1:
    # insert activeFiltersRender
    prop_val = "{hasAppliedFilters ? (<>\\n" + inner_chips + "</>) : null}"
    
    new_content = new_content[:ml_idx] + "<MusicReleaseList\n              activeFiltersRender=" + prop_val + new_content[ml_idx+17:]
    
with open('frontend/app/collection/page.tsx', 'w') as f:
    f.write(new_content)

print("Done patching page.tsx")
