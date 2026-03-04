import re

with open('frontend/app/components/MusicReleaseList.tsx', 'r') as f:
    orig_content = f.read()

content = orig_content.replace(
    '  pageSize?: number;',
    '  pageSize?: number;\n  activeFiltersRender?: React.ReactNode;'
)

content = content.replace(
    'export const MusicReleaseList = React.memo(function MusicReleaseList({ filters = {}, pageSize = 60, onSortChange }: MusicReleaseListProps & { onSortChange?: (f: MusicReleaseFilters) => void }) {',
    'export const MusicReleaseList = React.memo(function MusicReleaseList({ filters = {}, pageSize = 60, onSortChange, activeFiltersRender }: MusicReleaseListProps & { onSortChange?: (f: MusicReleaseFilters) => void }) {'
)

content = content.replace(
    '''          <div className="flex items-center gap-3 text-xs text-gray-500 flex-wrap mt-2">
            <span>{totalCount} release{totalCount !== 1 ? 's' : ''}</span>
          </div>''',
    '''          <div className="flex items-center gap-3 text-xs text-gray-500 flex-wrap mt-2 mb-4">
            <span>{totalCount} release{totalCount !== 1 ? 's' : ''}</span>
            {activeFiltersRender}
          </div>'''
)

with open('frontend/app/components/MusicReleaseList.tsx', 'w') as f:
    f.write(content)
