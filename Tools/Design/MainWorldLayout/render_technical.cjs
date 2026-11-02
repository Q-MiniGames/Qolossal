const fs=require('fs');const sharp=require('C:/Users/Qasim/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');const p='C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/MainWorldLayout_20261030/';
(async()=>{
 await sharp(p+'Qolossal_MainWorldLayout.svg').resize({width:4000}).png().toFile(p+'Qolossal_MainWorldLayout.png');
 await sharp(p+'Qolossal_MainWorldLayout.svg').resize({width:2200}).png().toFile(p+'preview.png');
 await sharp(p+'Qolossal_ArtUnderlay.svg').resize({width:4000}).png().toFile(p+'Qolossal_ArtUnderlay.png');
 const d=JSON.parse(fs.readFileSync(p+'world-map-data.json','utf8'));const svg=fs.readFileSync(p+'Qolossal_MainWorldLayout.svg','utf8');
 for(const id of ['MR01','MR06']){const r=d.regions.find(r=>r.id===id),[x,y,x2,y2]=r.focusBounds;const crop=svg.replace(/width="[^"]+" height="[^"]+" viewBox="[^"]+"/,`width="${x2-x}" height="${y2-y}" viewBox="${x} ${y} ${x2-x} ${y2-y}"`);await sharp(Buffer.from(crop)).resize({width:1400}).png().toFile(p+id+'_detail.png');}
 const h=fs.readFileSync(p+'index.html','utf8');new(require('vm').Script)(h.match(/<script>([\s\S]*?)<\/script>/)[1]);console.log('Rendered master, underlay and two region details; viewer syntax valid.');
})().catch(e=>{console.error(e);process.exit(1)});
