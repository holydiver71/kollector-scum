import re

with open('frontend/app/mockup/[id]/page.tsx', 'r') as f:
    mockup = f.read()

with open('frontend/app/page.tsx', 'r') as f:
    page = f.read()

# I will just write out the complete page.tsx using edit tool. Wait, edit tool is better for string replacement.
