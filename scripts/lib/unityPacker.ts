// https://qiita.com/asuuma/items/84be489cf39495a6be24

import tar from "tar-stream";
import zlib from "zlib";
import fs from "fs";
import path from "path";
import glob from "glob";

interface TarVFile {
  tarPath: string;
  buffer: Buffer;
}

interface TarFile {
  tarPath: string;
  filePath: string;
}
type TarFiles = Array<TarVFile | TarFile>;

export default async function packUnitypackage(
  rootPath: string,
  targetPath: string,
  output: string,
  recursive: boolean
) {
  const toRelative = relativePath(rootPath);
  const findMeta = recursive ? findMetaFilesRecursive : findMetaFilesFromAssets;
  const bundleFiles = findMeta([targetPath])
    .map(toRelative)
    .flatMap(addFile(rootPath));

  try {
    fs.unlinkSync(output);
  } catch (e) {}

  const pack = tar.pack();

  bundleFiles.forEach((bundle) => {
    if ("buffer" in bundle) {
      pack.entry({ name: bundle.tarPath }, bundle.buffer);
      return;
    }

    const data = fs.readFileSync(path.resolve(rootPath, bundle.filePath));
    pack.entry({ name: bundle.tarPath }, data);
  });

  pack.finalize();

  return new Promise((resolve) => {
    pack
      .pipe(zlib.createGzip())
      .pipe(fs.createWriteStream(output))
      .once("close", resolve);
  });
}

function fileNotExists(rootPath: string) {
  return function (file: string) {
    return !fs.existsSync(path.resolve(rootPath, file));
  };
}

function relativePath(rootPath: string) {
  return function (filePath: string): string {
    return path.relative(rootPath, filePath);
  };
}

function filePathIsNotEmpty(filePath: string): boolean {
  return filePath !== "" && filePath !== "." && filePath !== "./";
}

function addFile(rootPath: string) {
  const fileNotExistsRelative = fileNotExists(rootPath);
  return function (metaFilePath: string): TarFiles {
    if (fileNotExistsRelative(metaFilePath)) {
      throw new Error(`metadata not found: ${metaFilePath}`);
    }

    const assetFilePath = metaFilePath.slice(0, -5);
    if (fileNotExistsRelative(assetFilePath)) {
      throw new Error(`assets not found: ${assetFilePath}`);
    }

    // Note: meta file is not yaml file.
    // It has duplicate key.
    const data = fs.readFileSync(path.resolve(rootPath, metaFilePath), "utf8");
    const guid = extractGuid(data);

    const bundles: TarFiles = [
      {
        tarPath: `${guid}/asset.meta`,
        filePath: toPackagePath(metaFilePath),
      },
      {
        tarPath: `${guid}/pathname`,
        buffer: Buffer.from(toPackagePath(assetFilePath), "utf8"),
      },
    ];

    if (!fs.statSync(path.resolve(rootPath, assetFilePath)).isDirectory()) {
      bundles.push({
        tarPath: `${guid}/asset`,
        filePath: toPackagePath(assetFilePath),
      });
    }

    return bundles;
  };
}

function findMetaFilesRecursive(filePaths: string[]) {
  return uniq([
    ...findMetaFilesFromAssets(filePaths.filter(filePathIsNotEmpty)),
    ...filePaths.flatMap(findMetaFileRecursive),
  ]);
}

function findMetaFileRecursive(filePath: string) {
  return glob.sync(path.resolve(filePath, "**/*.meta"));
}

function findMetaFilesFromAssets(filePaths: string[]) {
  return filePaths.map(findMetaFileFromAsset);
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
    throw new Error("guid not found");
  }
  return matcher[1];
}

function toPackagePath(path: string) {
  return path.replaceAll("\\", "/");
}
