import { mkdirpSync } from "mkdirp";
import path from "path";
import cpx from "cpx";
import { rimrafSync } from "rimraf";
import Zip from "adm-zip";
import packUnitypackage from "./lib/unityPacker";
import removeAvaterId from "./lib/avaterIdRemover";
import { pascalCase } from "change-case";

const version = process.env.npm_package_version ?? "0.0.0";
const avaterName = process.env.npm_package_config_avater_name ?? "";
const targetAssetPath =
  process.env.npm_package_config_unity_package ?? pascalCase(avaterName);

async function main() {
  // clean
  rimrafSync([
    path.resolve(__dirname, "../dist"),
    path.resolve(__dirname, "../tmp"),
    path.resolve(__dirname, "../bundle"),
  ]);

  // init
  mkdirpSync(path.resolve(__dirname, "../dist"));
  mkdirpSync(path.resolve(__dirname, "../tmp"));
  mkdirpSync(path.resolve(__dirname, "../bundle"));

  cpx.copySync(
    path.resolve(__dirname, "../templates/README.txt"),
    path.resolve(__dirname, "../bundle")
  );
  cpx.copySync(
    path.resolve(__dirname, "../src/**/*"),
    path.resolve(__dirname, "../bundle/src")
  );

  cpx.copySync(
    path.resolve(__dirname, "../unity/Assets", targetAssetPath, "**/*"),
    path.resolve(__dirname, "../tmp/Assets", targetAssetPath)
  );
  cpx.copySync(
    path.resolve(__dirname, "../unity/Assets", `${targetAssetPath}.meta`),
    path.resolve(__dirname, "../tmp/Assets")
  );

  removeAvaterId(path.resolve(__dirname, "../tmp/Assets"));

  await packUnitypackage(
    path.resolve(__dirname, "../tmp"),
    path.resolve(__dirname, "../tmp/Assets", avaterName),
    path.resolve(__dirname, "../bundle/Avater.unitypackage"),
    true
  );

  const zip = new Zip();
  zip.addLocalFolder(path.resolve(__dirname, "../bundle"));
  zip.writeZip(
    path.resolve(__dirname, `../dist/${avaterName}-v${version}.zip`)
  );
}

main();
