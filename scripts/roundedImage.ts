import fs from "fs";
import path from "path";
import sharp from "sharp";

// 画像ファイルが保存されたディレクトリのパス
const imageDir = path.resolve(__dirname, "../tmp");

// 変換後の角丸の半径
const iamgeSize = 200;
const radius = 60;

// ディレクトリ内の画像ファイルを取得
fs.readdir(imageDir, (err, files) => {
  if (err) {
    console.error(err);
    return;
  }

  // 各画像ファイルに対して角丸加工を行う
  files.forEach((file) => {
    if (file.endsWith(".jpg") || file.endsWith(".png")) {
      // 画像ファイルを読み込む
      const inputPath = path.resolve(imageDir, file);
      const outputPath = path.resolve(imageDir, "rounded_" + file);
      sharp(inputPath)
        .resize({
          width: iamgeSize,
          height: iamgeSize,
          fit: sharp.fit.cover,
          position: sharp.strategy.entropy,
        })
        .composite([
          {
            input: Buffer.from(
              `<svg><rect x="0" y="0" width="${iamgeSize}" height="${iamgeSize}" rx="${radius}" ry="${radius}" /></svg>`
            ),
            blend: "dest-in",
          },
        ])
        .toFile(outputPath, (err, info) => {
          if (err) {
            console.error(err);
            return;
          }
          console.log(`Processed image: ${inputPath}`);
        });
    }
  });
});
