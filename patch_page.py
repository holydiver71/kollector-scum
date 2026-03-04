import re

with open("frontend/app/page.tsx", "r") as f:
    content = f.read()

# Remove the const RP block
content = re.sub(r'  /\* Static recently played for now since there\'s no backend for it \*/\n  const RP = \[.*?\n  \];\n', '', content, flags=re.DOTALL)

# Replace the Recent Played map with the real component
content = re.sub(r'          <div>\n            <h2 className="text-xs font-bold text-gray-500 uppercase tracking-widest mb-4 flex items-center gap-2">\n              <span className="text-base">🎵</span> Recently Played\n            </h2>\n            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">\n              \{RP\.map\(\(item, i\) => \{.*?\n              \}\)\}\n            </div>\n          </div>', r'''        <div>
          <RecentlyPlayed maxItems={24} />
        </div>''', content, flags=re.DOTALL)

with open("frontend/app/page.tsx", "w") as f:
    f.write(content)
