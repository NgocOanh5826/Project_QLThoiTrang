// Exercise the production filter handler with DOM-shaped controls (no browser).
import fs from 'node:fs';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const source=fs.readFileSync('Project_QLThoiTrang/wwwroot/js/site.js','utf8');
const start=source.indexOf("document.querySelectorAll('[data-admin-list-filter]')");
const end=source.indexOf("document.querySelectorAll('[data-admin-server-filter]')",start);
function control(value='') {return {value,listeners:{},addEventListener(k,fn){this.listeners[k]=fn;},dispatchEvent(e){this.listeners[e.type]?.();},focus(){}};}
const query=control(),state=control(),category=control(),reset=control(),count={};
state.dataset={adminRowFilter:'state'};category.dataset={adminRowFilter:'category',adminFilterAuto:'true'};category.options=[];category.append=(option)=>category.options.push(option);
function row(text,state,category,label){return {textContent:text,dataset:{state},getAttribute(name){return {'data-filter-category':category,'data-filter-category-label':label}[name]??null;}};}
const rows=[row('Áo khoác nữ','active','women','Nữ'),row('Đầm dự tiệc','inactive','women','Nữ'),row('Áo sơ mi nam','active','men','Nam')];
const empty={insertCell(){return {};}};
const table={tBodies:[{rows,insertRow(){return empty;}}],tHead:{rows:[{cells:[{},{}]}]}};
const toolbar={closest(){return {querySelector(){return table;}};},querySelectorAll(){return [state,category];},querySelector(s){return {'[data-admin-list-search]':query,'[data-admin-list-reset]':reset,'[data-admin-list-count]':count}[s];}};
vm.runInNewContext(source.slice(start,end),{document:{querySelectorAll(){return [toolbar];}},Option:class{constructor(text,value){this.text=text;this.value=value;}},Event:class{constructor(type){this.type=type;}}});
assert.equal(count.textContent,'3 / 3 bản ghi');assert.equal(category.options.length,2);assert.ok(category.options.some(o=>o.value==='women'&&o.text==='Nữ'));
query.value='ao khoac';query.listeners.input();assert.equal(rows[0].hidden,false);assert.equal(rows[1].hidden,true);assert.equal(rows[2].hidden,true);
query.value='dam';query.listeners.input();assert.equal(rows[1].hidden,false);
state.value='active';state.listeners.change();assert.equal(empty.hidden,false);
query.value='';query.listeners.input();category.value='women';category.listeners.change();assert.equal(rows[0].hidden,false);assert.equal(rows[1].hidden,true);assert.equal(rows[2].hidden,true);assert.equal(count.textContent,'1 / 3 bản ghi');
reset.listeners.click();assert.equal(query.value,'');assert.equal(state.value,'');assert.equal(category.value,'');assert.ok(rows.every(r=>!r.hidden));assert.equal(empty.hidden,true);
console.log('PASS: auto combobox options/labels, deduplication, Vietnamese search, combined selectors, empty state, count and reset');
