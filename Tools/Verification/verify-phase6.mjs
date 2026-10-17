import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

// Uses a previously imported Unity project's package references, read-only.
// This checks C# and pure NUnit rules; it does not simulate Unity's native runtime.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const referenceProject = path.resolve(process.argv[2] || root);
const unity = process.env.LOST_CITY_UNITY_CONTENTS ||
  '/Applications/Unity/Hub/Editor/2022.3.32f1/Unity.app/Contents';
const output = fs.mkdtempSync(path.join(os.tmpdir(), 'lost-city-verify-'));
const walk = dir => fs.existsSync(dir) ? fs.readdirSync(dir, { withFileTypes: true })
  .flatMap(entry => entry.isDirectory() ? walk(path.join(dir, entry.name)) : [path.join(dir, entry.name)]) : [];
const projectAssemblies = new Set(['Assembly-CSharp.dll', 'Assembly-CSharp-Editor.dll',
  ...walk(path.join(root, 'Assets')).filter(file => file.endsWith('.asmdef'))
    .map(file => JSON.parse(fs.readFileSync(file, 'utf8')).name + '.dll')]);
const quote = value => `"${value}"`;
const response = walk(path.join(referenceProject, 'Library/Bee/artifacts'))
  .find(file => path.basename(file) === 'Assembly-CSharp.rsp');
if (!response) throw new Error('An imported reference project with Library/Bee/Assembly-CSharp.rsp is required.');
const editorResponse = path.join(path.dirname(response), 'Assembly-CSharp-Editor.rsp');
const referenceLines = [response, editorResponse].filter(file => fs.existsSync(file))
  .flatMap(file => fs.readFileSync(file, 'utf8').split(/\r?\n/));
const references = referenceLines.filter(line => line.startsWith('-r:')).map(line => {
  const ref = line.slice(3).replace(/^"|"$/g, '');
  return path.isAbsolute(ref) ? ref : path.resolve(referenceProject, ref);
}).filter(ref => !projectAssemblies.has(path.basename(ref).replace(/\.ref\.dll$/, '.dll')) && fs.existsSync(ref));
const uniqueReferences = [...new Map(references.map(ref => [path.basename(ref), ref])).values()];
const defines = [...new Set(referenceLines.filter(line => line.startsWith('-define:')))];
const nunit = walk(path.join(referenceProject, 'Library/PackageCache'))
  .find(file => file.endsWith('/unity-custom/nunit.framework.dll'));
if (!nunit) throw new Error('Unity NUnit package DLL not found.');
const cs = directory => walk(path.join(root, directory)).filter(file => file.endsWith('.cs'));
function run(command, args) {
  const result = spawnSync(command, args, { cwd: root, encoding: 'utf8' });
  if (result.stdout) process.stdout.write(result.stdout);
  if (result.stderr) process.stderr.write(result.stderr);
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${path.basename(command)} exited ${result.status}`);
}
function compile(name, sources, extra = [], executable = false) {
  if (!sources.length) return null;
  const target = path.join(output, name + (executable ? '.exe' : '.dll'));
  const args = ['-nologo', '-nowarn:0649', '-langversion:9.0', '-nostdlib+', '-target:' + (executable ? 'exe' : 'library'),
    '-out:' + quote(target), ...defines,
    ...[...new Set([...uniqueReferences, ...extra])].map(ref => '-r:' + quote(ref)), ...sources.map(quote)];
  const rsp = path.join(output, name + '.rsp');
  fs.writeFileSync(rsp, args.join('\n'));
  run(path.join(unity, 'NetCoreRuntime/dotnet'), [path.join(unity, 'DotNetSdkRoslyn/csc.dll'), '@' + rsp]);
  console.log(`Compiled ${name} (${sources.length} files).`);
  return target;
}
console.log(`Verification output: ${output}`);
const core = compile('LostCity.Meta.Core', cs('Assets/_Project/Code/Meta/Core'));
const coreTests = compile('LostCity.Meta.Core.Tests', cs('Assets/_Project/Tests/EditMode/Meta')
  .filter(file => /\/(SaveGameDataTests|CaseProgressionRulesTests)\.cs$/.test(file)), [core, nunit]);
fs.copyFileSync(nunit, path.join(output, 'nunit.framework.dll'));
const runner = compile('CoreTestRunner', [path.join(root, 'Tools/Verification/CoreTestRunner.cs')], [nunit], true);
run(path.join(unity, 'MonoBleedingEdge/bin/mono'), [runner, coreTests, path.join(output, 'core-results.xml')]);
const persistence = compile('LostCity.Meta.Persistence', cs('Assets/_Project/Code/Meta/Persistence'), [core]);
const deps = [core, persistence].filter(Boolean);
const runtime = compile('Assembly-CSharp', cs('Assets/_Project/Code')
  .filter(file => !file.includes('/Editor/') && !file.includes('/Meta/Core/') && !file.includes('/Meta/Persistence/')), deps);
const editor = compile('Assembly-CSharp-Editor', cs('Assets/_Project/Code').filter(file => file.includes('/Editor/')), [...deps, runtime]);
compile('LostCity.Meta.Tests.CompileCheck', cs('Assets/_Project/Tests'), [...deps, runtime, editor, nunit].filter(Boolean));
console.log('PASS: pure core tests and C# compilation. Unity runtime/serialization/scene checks still require the Editor.');
