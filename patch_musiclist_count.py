import re

with open('frontend/app/components/MusicReleaseList.tsx', 'r') as f:
    orig_content = f.read()

content = orig_content.replace(
    '          {searchParams?.get(\'showAdvanced\') === \'true\' && (',
    '''          <div className="flex items-center gap-3 text-xs text-gray-500 flex-wrap mt-2">
            <span>{totalCount} release{totalCount !== 1 ? 's' : ''}</span>
          </div>

          {searchParams?.get(\'showAdvanced\') === \'true\' && ('''
)

with open('frontend/app/components/MusicReleaseList.tsx', 'w') as f:
    f.write(content)
