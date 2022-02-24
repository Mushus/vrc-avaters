import fs from 'fs';
import path from 'path';
import micromatch from 'micromatch';
import { google } from 'googleapis';
import { program, Command } from 'commander';

interface Options {
  exclude: string;
  output: string;
  fileId: string;
}

program
  .name('importer')
  .description('Import file from Google Drive')
  .version('0.1.0')
  .option('-f, --file-id <string>', 'File id of the Google Drive to import from', true)
  .option('-e, --exclude <string>', 'Exclude file pattern', '')
  .option('-o, --output <string>', 'Output directory path')
  .action(async ({ fileId, exclude, output }: Options, { args }: Command) => {
    const auth = await google.auth.getClient({
      scopes: [
        'https://www.googleapis.com/auth/compute',
        'https://www.googleapis.com/auth/drive.readonly',
        'https://www.googleapis.com/auth/drive.metadata.readonly',
      ]
    });

    const drive = google.drive({ version: 'v3', auth });

    const filesRes = await drive.files.list({ q: `'${fileId}' in parents` });
    const files = filesRes.data.files as { name: string; id: string }[];

    const ignoreFiles = ignoreFilesWithPattern(exclude);

    await Promise.all(
      ignoreFiles(files).map(
        async ({ id, name }) => {
          const files = path.join(output, name);
          const writer = fs.createWriteStream(files);
          const res = await drive.files.get({ fileId: id, alt: 'media' }, { responseType: 'stream' })
          res.data.pipe(writer);
        }
      )
    );
  });

program.parse();


function ignoreFilesWithPattern(pattern: string): <T extends { name: string }>(files: T[]) => T[] {
  if (pattern === '') {
    return <T extends { name: string }>(files: T[]) => files
  }

  return function <T extends { name: string }>(files: T[]) {
    const names = files.map(({ name }) => name);
    const nameSet = new Set(micromatch(names, pattern));
    return files.filter(file => !nameSet.has(file.name));
  }
}

