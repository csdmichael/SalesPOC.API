const fs = require('fs');
const spec = JSON.parse(fs.readFileSync('swagger.json', 'utf8'));

// Fix 1: Parameters missing 'type' field (required in Swagger 2.0 for query/path params)
let paramFixes = 0;
for (const [path, methods] of Object.entries(spec.paths || {})) {
  for (const [method, op] of Object.entries(methods)) {
    if (!op || !op.parameters) continue;
    for (const param of op.parameters) {
      if ((param.in === 'query' || param.in === 'path') && !param.type) {
        if (param.format === 'int32' || param.format === 'int64') {
          param.type = 'integer';
        } else if (param.format === 'double' || param.format === 'float') {
          param.type = 'number';
        } else {
          param.type = 'string';
        }
        paramFixes++;
      }
    }
  }
}
console.log(`Fixed ${paramFixes} parameters missing 'type'`);

// Fix 2: Replace 'nullable' with 'x-nullable' everywhere in definitions
let nullableFixes = 0;
function fixNullable(obj) {
  if (typeof obj !== 'object' || obj === null) return;
  if (Array.isArray(obj)) {
    obj.forEach(fixNullable);
    return;
  }
  if ('nullable' in obj) {
    obj['x-nullable'] = obj.nullable;
    delete obj.nullable;
    nullableFixes++;
  }
  for (const val of Object.values(obj)) {
    fixNullable(val);
  }
}
fixNullable(spec.definitions);

// Fix 3: Replace 'oneOf' with '$ref' for nullable refs in definitions
let oneOfFixes = 0;
function fixOneOf(obj) {
  if (typeof obj !== 'object' || obj === null) return;
  if (Array.isArray(obj)) {
    obj.forEach(fixOneOf);
    return;
  }
  for (const [key, val] of Object.entries(obj)) {
    if (val && typeof val === 'object' && val.oneOf && Array.isArray(val.oneOf)) {
      const refEntry = val.oneOf.find(e => e['$ref']);
      if (refEntry) {
        val['$ref'] = refEntry['$ref'];
        val['x-nullable'] = true;
        delete val.oneOf;
        oneOfFixes++;
      }
    }
    if (typeof val === 'object' && val !== null) {
      fixOneOf(val);
    }
  }
}
fixOneOf(spec.definitions);

console.log(`Fixed ${nullableFixes} 'nullable' -> 'x-nullable'`);
console.log(`Fixed ${oneOfFixes} 'oneOf' -> '$ref'`);

fs.writeFileSync('swagger.json', JSON.stringify(spec, null, 2) + '\n');
console.log('swagger.json written successfully');
