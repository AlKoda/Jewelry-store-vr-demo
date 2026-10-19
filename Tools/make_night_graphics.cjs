// Original vector art, no external font or image inputs. Node + sharp.
// Run from repository root: node Tools/make_night_graphics.cjs
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '..');
const out = path.join(root, 'Prototype/ThirdParty/NightGraphics');
fs.mkdirSync(out, { recursive: true });
// A compact original monoline alphabet; all SVG text is paths, with no font dependency.
const glyph = {
 A:'0,10 3,0 6,10|1,6 5,6', B:'0,10 0,0 4,0 6,2 4,5 0,5|4,5 6,7 6,8 4,10 0,10',
 C:'6,1 4,0 2,0 0,2 0,8 2,10 4,10 6,9', D:'0,10 0,0 3,0 6,3 6,7 3,10 0,10',
 E:'6,0 0,0 0,10 6,10|0,5 5,5', F:'6,0 0,0 0,10|0,5 5,5',
 G:'6,2 4,0 2,0 0,2 0,8 2,10 6,10 6,5 3,5', H:'0,0 0,10|6,0 6,10|0,5 6,5',
 I:'0,0 6,0|3,0 3,10|0,10 6,10', J:'0,0 6,0 6,8 4,10 2,10 0,8',
 K:'0,0 0,10|6,0 0,6|2,4 6,10', L:'0,0 0,10 6,10', M:'0,10 0,0 3,5 6,0 6,10',
 N:'0,10 0,0 6,10 6,0', O:'2,0 4,0 6,2 6,8 4,10 2,10 0,8 0,2 2,0',
 P:'0,10 0,0 4,0 6,2 6,3 4,5 0,5', Q:'2,0 4,0 6,2 6,8 4,10 2,10 0,8 0,2 2,0|3,7 7,11',
 R:'0,10 0,0 4,0 6,2 6,3 4,5 0,5|3,5 6,10', S:'6,1 4,0 2,0 0,2 0,3 6,7 6,8 4,10 2,10 0,9',
 T:'0,0 6,0|3,0 3,10', U:'0,0 0,8 2,10 4,10 6,8 6,0', V:'0,0 3,10 6,0',
 W:'0,0 1,10 3,5 5,10 6,0', X:'0,0 6,10|6,0 0,10', Y:'0,0 3,5 6,0|3,5 3,10',
 Z:'0,0 6,0 0,10 6,10', '&':'6,10 0,3 0,1 2,0 4,1 4,3 0,7 0,9 2,10 4,9 6,6',
 '0':'2,0 4,0 6,2 6,8 4,10 2,10 0,8 0,2 2,0|1,8 5,2',
 '1':'1,2 3,0 3,10|0,10 6,10', '2':'0,2 2,0 4,0 6,2 6,4 0,10 6,10',
 '3':'0,0 6,0 3,5 6,7 6,8 4,10 0,10', '4':'5,10 5,0 0,7 6,7',
 '5':'6,0 0,0 0,5 4,5 6,7 6,8 4,10 0,10', '6':'6,0 2,0 0,3 0,8 2,10 4,10 6,8 6,6 4,5 0,5',
 '7':'0,0 6,0 2,10', '8':'2,0 4,0 6,2 6,3 0,7 0,8 2,10 4,10 6,8 6,7 0,3 0,2 2,0',
 '9':'6,5 2,5 0,3 0,2 2,0 4,0 6,2 6,8 4,10 0,10', '-':'0,5 6,5'
};
function text(s,cx,y,height,color='#c5a776',weight=.42) {
 const scale=height/10, width=(s.length*10-4)*scale;
 return `<g stroke="${color}" fill="none" stroke-width="${weight}" stroke-linecap="round" stroke-linejoin="round" transform="translate(${cx-width/2},${y}) scale(${scale})">` +
 [...s].map((c,i)=>c===' '?'':`<g transform="translate(${i*10},0)">${glyph[c].split('|').map(p=>`<polyline points="${p}"/>`).join('')}</g>`).join('')+'</g>';
}
const svg=(w,h,b)=>`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${b}</svg>`;
const plate=(w,h)=>`<rect width="${w}" height="${h}" fill="#182321"/><rect x="12" y="12" width="${w-24}" height="${h-24}" rx="2" fill="none" stroke="#a58b62" stroke-width="2"/>`;
const files=[];
async function save(name,w,h,body){
 const source=svg(w,h,body); fs.writeFileSync(path.join(out,name+'.svg'),source);
 await sharp(Buffer.from(source)).png().toFile(path.join(out,name+'.png')); files.push({name,width:w,height:h});
}
async function main(){
 await save('ShopFascia',1024,256,plate(1024,256)+
 '<g fill="none" stroke="#c5a776" stroke-width="2"><path d="M496 37 L512 25 528 37 512 53 Z M496 37 H528 M512 25 L506 37 512 53 518 37 Z"/></g>'+
 text('AUREL & FINCH',512,84,52)+text('FINE JEWELLERY',512,177,19));
 await save('CollectionRings',512,256,plate(512,256)+'<circle cx="256" cy="75" r="25" fill="none" stroke="#c5a776" stroke-width="3"/>'+text('RINGS',256,131,32)+text('THE COLLECTION',256,198,13));
 await save('CollectionNecklaces',512,256,plate(512,256)+'<path d="M230 49 Q215 108 256 111 Q297 108 282 49 M256 111 l-7 10 7 10 7-10 Z" stroke="#c5a776" fill="none" stroke-width="3"/>'+text('NECKLACES',256,147,28));
 await save('Closed',512,256,plate(512,256)+text('CLOSED',256,69,46)+text('PLEASE VISIT AGAIN',256,161,17));
 await save('StaffOnly',512,256,plate(512,256)+text('STAFF ONLY',256,89,32));
 await save('SceneTape',1024,128,'<rect width="1024" height="128" fill="#edc642"/>'+text('SCENE - DO NOT CROSS',512,43,38,'#171d1c',.65));
 // 4x2 numbered label atlas; UV rectangles recorded separately for marker faces.
 let markers=''; for(let i=0;i<8;i++){let x=(i%4)*128,y=Math.floor(i/4)*128;markers+=`<rect x="${x}" y="${y}" width="128" height="128" fill="#edc642"/>`+text(String(i+1),x+64,y+28,70,'#171d1c',.72);}
 await save('MarkerNumbers',512,256,markers);
 // Generic stylized outsole. It is not a brand, forensic exemplar or UV trace.
 let tread='<g fill="#655a4b" fill-opacity="0.58">';
 for(let row=0;row<7;row++){let yy=60+row*27; let inset=row<2?22-row*10:row>4?(row-4)*9:0;tread+=`<path d="M${174+inset} ${yy} l62 12 -4 15 -62-12 Z M${260} ${yy+12} l${66-inset} -12 4 15 -${66-inset} 12 Z"/>`;}
 tread+='<path d="M180 322 Q256 298 323 326 L313 407 Q257 447 190 407 Z"/></g>';
 await save('ShoeTrace_Generic',512,512,tread);
 await save('Scuff_Generic',512,256,'<g stroke="#534c44" stroke-linecap="round" fill="none" opacity=".45"><path stroke-width="7" d="M83 170 Q244 90 427 98"/><path stroke-width="3" d="M103 182 Q263 105 402 113 M95 154 Q204 99 366 87 M135 175 Q255 119 373 121"/></g>');
 await save('DustSmear_Generic',512,256,'<g fill="#978777" opacity=".20"><ellipse cx="254" cy="132" rx="176" ry="47"/><ellipse cx="224" cy="115" rx="142" ry="34"/><ellipse cx="290" cy="148" rx="115" ry="20"/></g>');
 fs.writeFileSync(path.join(out,'graphics.json'),JSON.stringify({author:'Original project vector artwork generated with Codex',fictionalShop:'Aurel & Finch',files,markerAtlas:{columns:4,rows:2,order:'1–4 top, 5–8 bottom',cellPixels:128,paddingPixels:16},decals:'Straight alpha; no emission or UV interpretation'},null,2)+'\n');
 const tiles=await Promise.all(files.map(async(f,i)=>({input:await sharp(path.join(out,f.name+'.png')).resize(480,210,{fit:'contain',background:'#ddd9d0'}).png().toBuffer(),left:(i%2)*500+10,top:Math.floor(i/2)*230+10})));
 const preview=path.join(root,'Docs/History/AssetPreparation');fs.mkdirSync(preview,{recursive:true});
 await sharp({create:{width:1000,height:Math.ceil(files.length/2)*230,channels:3,background:'#ddd9d0'}}).composite(tiles).png().toFile(path.join(preview,'graphics-preview.png'));
 console.log(`Prepared ${files.length} SVG/PNG pairs.`);
}
main().catch(e=>{console.error(e);process.exit(1);});
