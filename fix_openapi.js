const fs = require('fs');
const spec = JSON.parse(fs.readFileSync('openapi.json', 'utf8'));
let pf = 0, nf = 0, of2 = 0;

// Fix 1: Add missing 'type' to query/path params
for (const [p, methods] of Object.entries(spec.paths || {})) {
  for (const [m, op] of Object.entries(methods)) {
    if (!op || !op.parameters) continue;
    for (const param of op.parameters) {
      if ((param.in === 'query' || param.in === 'path') && !param.type) {
        if (param.format === 'int32' || param.format === 'int64') param.type = 'integer';
        else if (param.format === 'double' || param.format === 'float') param.type = 'number';
        else param.type = 'string';
        pf++;
      }
    }
  }
}

// Fix 2: Replace 'nullable' with 'x-nullable'
function fixNullable(obj) {
  if (typeof obj !== 'object' || obj === null) return;
  if (Array.isArray(obj)) { obj.forEach(fixNullable); return; }
  if ('nullable' in obj) { obj['x-nullable'] = obj.nullable; delete obj.nullable; nf++; }
  for (const val of Object.values(obj)) fixNullable(val);
}
fixNullable(spec.definitions);

// Fix 3: Replace 'oneOf' with direct $ref
function fixOneOf(obj) {
  if (typeof obj !== 'object' || obj === null) return;
  if (Array.isArray(obj)) { obj.forEach(fixOneOf); return; }
  for (const [key, val] of Object.entries(obj)) {
    if (val && typeof val === 'object' && val.oneOf && Array.isArray(val.oneOf)) {
      const refEntry = val.oneOf.find(function(e) { return e['$ref']; });
      if (refEntry) {
        val['$ref'] = refEntry['$ref'];
        val['x-nullable'] = true;
        delete val.oneOf;
        of2++;
      }
    }
    if (typeof val === 'object' && val !== null) fixOneOf(val);
  }
}
fixOneOf(spec.definitions);

fs.writeFileSync('openapi.json', JSON.stringify(spec, null, 2) + '\n');
console.log('Fixed params: ' + pf + ', nullable: ' + nf + ', oneOf: ' + of2);
