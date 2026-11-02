from pathlib import Path
p=Path('Tools/Design/ConnectedWorldMap_20261029/draw_world.py');s=p.read_text(encoding='utf-8-sig')
s=s.replace('geom=[];cavelines=[];landmarks=[]','geom=[];cavelines=[];landmarks=[];surfaceRooms=[]')
s=s.replace("boxes[n['id']]=(x-17,y-17,34,34)",'''bx=x0+(b[0]-minx)*sx;by=660-(b[1]+b[3]-miny)*sy;bw=b[2]*sx;bh=b[3]*sy
  boxes[n['id']]=(bx,by,bw,bh)
  k=min(8,bh/5);d=f'M{bx+k} {by} H{bx+bw-k} L{bx+bw} {by+k} V{by+bh-k} L{bx+bw-k} {by+bh} H{bx+k} L{bx} {by+bh-k} V{by+k} Z'
  surfaceRooms.append(f'<g class="node room" data-id="{n["id"]}"><path d="{d}" fill="{COLORS[i]}" fill-opacity=".2" stroke="{COLORS[i]}" stroke-width="3"/></g>')''')
s=s.replace('stroke-width:53','stroke-width:17').replace('stroke-width:45','stroke-width:11')
s=s.replace("''.join(geom)+'</g><g id=\"passages\">'","''.join(geom+surfaceRooms)+'</g><g id=\"passages\">'")
s=s.replace("(OUT/'Qolossal_ConnectedWorld_NoLabels.svg').write_text(base+'</svg>')","(OUT/'Qolossal_ConnectedWorld_NoRoomLabels.svg').write_text(base+'</svg>',encoding='utf-8')\n(OUT/'Qolossal_ConnectedWorld_Geometry.svg').write_text(base.replace(ornaments,'')+'</svg>',encoding='utf-8')")
s=s.replace('.write_text(svg)',".write_text(svg,encoding='utf-8')")
p.write_text(s,encoding='utf-8')
