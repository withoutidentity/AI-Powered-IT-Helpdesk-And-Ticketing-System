import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(dirname(fileURLToPath(import.meta.url)));
const envPath = join(root, '.env');
const outputPath = join(root, 'src', 'app', 'environments', 'environment.ts');

const values = readEnv(envPath);
const apiBaseUrl = values.NG_APP_API_BASE_URL ?? 'http://localhost:5175/api/v1';

mkdirSync(dirname(outputPath), { recursive: true });
writeFileSync(
  outputPath,
  `export const environment = {\n  apiBaseUrl: ${JSON.stringify(apiBaseUrl)},\n};\n`,
);

function readEnv(path) {
  const values = {};
  try {
    const content = readFileSync(path, 'utf8');
    for (const rawLine of content.split(/\r?\n/)) {
      const line = rawLine.trim();
      if (!line || line.startsWith('#')) continue;
      const separatorIndex = line.indexOf('=');
      if (separatorIndex <= 0) continue;
      const key = line.slice(0, separatorIndex).trim();
      const value = unquote(line.slice(separatorIndex + 1).trim());
      values[key] = value;
    }
  } catch (error) {
    if (error.code !== 'ENOENT') throw error;
  }
  return values;
}

function unquote(value) {
  if (
    value.length >= 2 &&
    ((value.startsWith('"') && value.endsWith('"')) ||
      (value.startsWith("'") && value.endsWith("'")))
  ) {
    return value.slice(1, -1);
  }
  return value;
}