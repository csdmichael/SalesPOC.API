const fs = require('fs');
const oas3 = JSON.parse(fs.readFileSync('openapi.json', 'utf8'));

const swagger2 = {
  swagger: '2.0',
  info: oas3.info
};

if (oas3.servers && oas3.servers.length > 0) {
  const url = new URL(oas3.servers[0].url);
  swagger2.host = url.host;
  swagger2.basePath = url.pathname === '/' ? '/' : url.pathname;
  swagger2.schemes = [url.protocol.replace(':', '')];
}

swagger2.consumes = ['application/json'];
swagger2.produces = ['application/json', 'text/json', 'text/plain'];

function fixRef(obj) {
  if (!obj || typeof obj !== 'object') return obj;
  const result = Array.isArray(obj) ? [...obj] : { ...obj };
  if (result['$ref']) {
    result['$ref'] = result['$ref'].replace('#/components/schemas/', '#/definitions/');
  }
  for (const key of Object.keys(result)) {
    if (typeof result[key] === 'object' && result[key] !== null) {
      result[key] = fixRef(result[key]);
    }
  }
  return result;
}

function flattenSchema(schema) {
  const result = {};
  if (schema.type) result.type = schema.type;
  if (schema.format) result.format = schema.format;
  if (schema.enum) result.enum = schema.enum;
  if (schema.default !== undefined) result.default = schema.default;
  if (schema.minimum !== undefined) result.minimum = schema.minimum;
  if (schema.maximum !== undefined) result.maximum = schema.maximum;
  if (schema.items) result.items = fixRef(schema.items);
  return result;
}

swagger2.paths = {};
for (const [path, methods] of Object.entries(oas3.paths || {})) {
  swagger2.paths[path] = {};
  for (const [method, op] of Object.entries(methods)) {
    if (!['get', 'post', 'put', 'delete', 'patch', 'options', 'head'].includes(method)) continue;
    const newOp = {};
    if (op.tags) newOp.tags = op.tags;
    if (op.operationId) newOp.operationId = op.operationId;

    const params = [];
    if (op.parameters) {
      for (const p of op.parameters) {
        const np = { name: p.name, in: p.in };
        if (p.required !== undefined) np.required = p.required;
        if (p.description) np.description = p.description;
        if (p.schema) {
          if (p.in === 'body') {
            np.schema = fixRef(p.schema);
          } else {
            Object.assign(np, flattenSchema(p.schema));
          }
        }
        params.push(np);
      }
    }

    if (op.requestBody) {
      const content = op.requestBody.content;
      const mediaType = content['application/json'] || content[Object.keys(content)[0]];
      if (mediaType && mediaType.schema) {
        params.push({
          name: 'body',
          in: 'body',
          required: op.requestBody.required || false,
          schema: fixRef(mediaType.schema)
        });
      }
    }

    if (params.length > 0) newOp.parameters = params;

    newOp.responses = {};
    for (const [code, resp] of Object.entries(op.responses || {})) {
      const newResp = { description: resp.description || '' };
      if (resp.content) {
        const ct = resp.content['application/json'] || resp.content[Object.keys(resp.content)[0]];
        if (ct && ct.schema) {
          newResp.schema = fixRef(ct.schema);
        }
      }
      newOp.responses[code] = newResp;
    }

    swagger2.paths[path][method] = newOp;
  }
}

if (oas3.components && oas3.components.schemas) {
  swagger2.definitions = {};
  for (const [name, schema] of Object.entries(oas3.components.schemas)) {
    swagger2.definitions[name] = fixRef(schema);
  }
}

const json = JSON.stringify(swagger2, null, 2);
fs.writeFileSync('openapi.json', json);
fs.writeFileSync('swagger.json', json);
console.log(`Done. swagger: ${swagger2.swagger}, paths: ${Object.keys(swagger2.paths).length}, definitions: ${Object.keys(swagger2.definitions || {}).length}`);
