// https://qiita.com/asuuma/items/84be489cf39495a6be24

import { program, Command } from 'commander';
import tar from 'tar-stream';
import zlib from 'zlib';
import fs from 'fs';
import path from 'path';
import glob from 'glob';

interface Options {
  recursive: boolean;
  output: string;
}

interface TarVFile {
  tarPath: string;
  buffer: Buffer;
}

interface TarFile {
  tarPath: string;
  filePath: string;
}
type TarFiles = Array<TarVFile | TarFile>;

program
  .name('unitypacker')
  .description('create unitypackage')
  .version('0.1.0')
  .option('-r --recursive', 'bundle recursive', true)
  .option('-o, --output <string>', 'Output unitypackage path', 'output.unitypackage')
  .action(async ({ recursive, output }: Options, { args }: Command) => {
    if (args.some(fileNotExists)) {
      console.log('target not found');
      return;
    }

    const toRelative = relativePath(process.cwd())
    const findMeta = recursive ? findMetaFilesRecursive : findMetaFilesFromAssets;
    const bundleFiles = findMeta(args).map(toRelative).flatMap(addFile)

    try {
      fs.unlinkSync(output);
    } catch (e) { }

    const pack = tar.pack();

    bundleFiles.forEach((bundle) => {
      if ('buffer' in bundle) {
        pack.entry({ name: bundle.tarPath }, bundle.buffer);
        return;
      }

      const data = fs.readFileSync(bundle.filePath);
      pack.entry({ name: bundle.tarPath }, data);
    });

    pack.finalize();

    await pack
      .pipe(zlib.createGzip())
      .pipe(fs.createWriteStream(output));
  })

program.parse();


function fileNotExists(file: string) {
  return !fs.existsSync(file)
}

function relativePath(rootPath: string) {
  return function (filePath: string): string {
    return path.relative(rootPath, filePath);
  }
}

function filePathIsNotEmpty(filePath: string): boolean {
  return filePath !== '' && filePath !== '.' && filePath !== './';
}

function addFile(metaFilePath: string): TarFiles {
  if (fileNotExists(metaFilePath)) {
    throw new Error(`metadata not found: ${metaFilePath}`);
  }

  const assetFilePath = metaFilePath.slice(0, -5);
  if (fileNotExists(assetFilePath)) {
    throw new Error(`assets not found: ${assetFilePath}`);
  }

  // Note: meta file is not yaml file.
  // It has duplicate key.
  const data = fs.readFileSync(metaFilePath, 'utf8');
  const guid = extractGuid(data);

  const bundles: TarFiles = [
    { tarPath: `${guid}/asset.meta`, filePath: metaFilePath },
    { tarPath: `${guid}/pathname`, buffer: Buffer.from(assetFilePath, 'utf8') }
  ];

  if (!fs.statSync(assetFilePath).isDirectory()) {
    bundles.push({ tarPath: `${guid}/asset`, filePath: assetFilePath });
  }

  return bundles;
}

function findMetaFilesRecursive(filePaths: string[]) {
  return uniq([
    ...findMetaFilesFromAssets(filePaths.filter(filePathIsNotEmpty)),
    ...filePaths.flatMap(findMetaFileRecursive)
  ]);
}

function findMetaFileRecursive(filePath: string) {
  return glob.sync(path.resolve(filePath, '**/*.meta'));
}

function findMetaFilesFromAssets(filePaths: string[]) {
  return filePaths.map(findMetaFileFromAsset)
}

function findMetaFileFromAsset(filePath: string) {
  return `${filePath}.meta`;
}

function uniq<T>(array: T[]): T[] {
  return Array.from(new Set(array));
}

function extractGuid(data: string): string {
  const matcher = data.match(/^guid:\s([0-9a-f]+)$/m);
  if (!matcher) {
    throw new Error('guid not found');
  }
  return matcher[1];
}
