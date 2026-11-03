// Exercise the production filter handler with DOM-shaped controls (no browser).
import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source=fs.readFileSync('Project_QLThoiTrang/wwwroot/js/site.js','utf8');
const start=source.indexOf("document.querySelectorAll('[data-admin-list-filter]')");
const end=source.indexOf('// Shared select-only comboboxes',start);
function control(value='') {return {value,listeners:{},addEventListener(k,fn){this.listeners[k]=fn;},dispatchEvent(e){this.listeners[e.type]?.();},focus(){}};}
const query=control(),state=control(),reset=control(),count={};
const rows=[{textContent:'Áo khoác nữ',dataset:{state:'active'}},{textContent:'Đầm dự tiệc',dataset:{state:'inactive'}}];
const empty={insertCell(){return {};}};
const table={tBodies:[{rows,insertRow(){return empty;}}],tHead:{rows:[{cells:[{},{}]}]}};
const toolbar={closest(){return {querySelector(){return table;}};},querySelector(s){return {'[data-admin-list-search]':query,'[data-admin-list-state]':state,'[data-admin-list-reset]':reset,'[data-admin-list-count]':count}[s];}};
vm.runInNewContext(source.slice(start,end),{document:{querySelectorAll(){return [toolbar];}},Event:class{constructor(type){this.type=type;}}});
assert.equal(count.textContent,'2 / 2 bản ghi');
query.value='ao khoac';query.listeners.input();assert.equal(rows[0].hidden,false);assert.equal(rows[1].hidden,true);
query.value='dam';query.listeners.input();assert.equal(rows[1].hidden,false);
state.value='active';state.listeners.change();assert.equal(empty.hidden,false);
reset.listeners.click();assert.equal(query.value,'');assert.equal(state.value,'');assert.ok(rows.every(r=>!r.hidden));assert.equal(empty.hidden,true);
console.log('PASS: initial count, Vietnamese search, combined status, empty state, reset');
