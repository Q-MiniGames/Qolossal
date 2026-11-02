from pathlib import Path
p=Path('Tools/Design/MossPocketMap_20261029/draw_map.py')
s=p.read_text(encoding='utf-8-sig')
s=s.replace("body=f'<polygon", "body=f'<defs><clipPath id=\"roomClip\"><rect width=\"{W}\" height=\"{H}\"/></clipPath></defs><g clip-path=\"url(#roomClip)\"><polygon")
s=s.replace("(art/'MR01_C02_MapGeometry.svg').write_text", "body+='</g>'\n(art/'MR01_C02_MapGeometry.svg').write_text")
s=s.replace("text(108,333,'Passage continues'", "text(110,385,'Passage continues'")
s=s.replace("preview+='</svg>'", "preview+=text(90,1012,'All discoveries shown',17,sage)\npreview=re.sub(r'(<text[^>]*)(>MR01[^<]*</text>)',lambda m:m.group(1).replace('Georgia,serif','Segoe UI,sans-serif')+m.group(2),preview)\npreview+='</svg>'")
p.write_text(s,encoding='utf-8')
