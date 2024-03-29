import { program, Command } from "commander";
import glob from "glob";
import fs from "fs";
import path from "path";

interface Options {}

program
  .name("Avater ID Remover")
  .description("Remove avater ID from scene")
  .version("0.1.0")
  .action(async ({}: Options, { args }: Command) => {
    const [target] = args;
    const sceneFilePaths = ["unity", "prefab"].flatMap((ext) =>
      glob.sync(path.join(target, `/**/*.${ext}`))
    );
    sceneFilePaths.forEach((sceneFilePath) => {
      let data = fs.readFileSync(sceneFilePath).toString("utf8");
      data = data.replace(/blueprintId:\savtr_[0-9a-f\-]+$/gm, "blueprintId:");
      fs.writeFileSync(sceneFilePath, data);
    });
  });

program.parse();
