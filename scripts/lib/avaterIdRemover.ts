import glob from "glob";
import fs from "fs";
import path from "path";

export default function removeAvaterId(target: string) {
  const sceneFilePaths = glob.sync(path.join(target, `/**/*.@(unity|prefab)`));
  sceneFilePaths.forEach((sceneFilePath) => {
    let data = fs.readFileSync(sceneFilePath).toString("utf8");
    data = data.replace(/blueprintId:\savtr_[0-9a-f\-]+$/gm, "blueprintId:");
    fs.writeFileSync(sceneFilePath, data);
  });
}
