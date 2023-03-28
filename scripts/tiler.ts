import sharp from "sharp";
import fs from "fs";
import path from "path";

const folderPath = path.resolve(__dirname, "../tmp");
const outputPath = path.resolve(__dirname, "../tmp/tile.png");
const tileSize = 256; // タイルのサイズ（ピクセル単位）

async function processImages(): Promise<void> {
  const fileNames = fs.readdirSync(folderPath);
  const imageFileNames = fileNames.filter((fileName) =>
    /\.(jpg|jpeg|png)$/i.test(fileName)
  );
  const numImages = imageFileNames.length;
  const numTilesPerRow = Math.ceil(Math.sqrt(numImages));
  const numTilesPerCol = Math.ceil(numImages / numTilesPerRow);
  const outputWidth = numTilesPerRow * tileSize;
  const outputHeight = numTilesPerCol * tileSize;

  const imageBuffers = imageFileNames.map((fileName) =>
    sharp(path.join(folderPath, fileName)).resize(tileSize, tileSize).toBuffer()
  );

  const buffers = await Promise.all(imageBuffers);

  const outputImage = sharp({
    create: {
      width: outputWidth,
      height: outputHeight,
      channels: 3,
      background: { r: 128, g: 128, b: 128 },
    },
  });

  outputImage.composite([
    ...buffers.map((buffer, index) => {
      const tileX = index % numTilesPerRow;
      const tileY = Math.floor(index / numTilesPerRow);
      const tileOffsetX = tileX * tileSize;
      const tileOffsetY = tileY * tileSize;
      return {
        input: buffer,
        left: tileOffsetX,
        top: tileOffsetY,
      };
    }),
    {
      input: Buffer.from(
        `<svg
          width="${outputWidth}"
          height="${outputHeight}"
          viewBox="0, 0, ${outputWidth}, ${outputHeight}">
            <text x="${outputWidth - 20}" y="${
          outputHeight - 20
        }" font-family="'M+ 1p'" fill="black" font-size="72" text-anchor="end">表情${numImages}種類</text>
          </svg>`
      ),
      gravity: "southeast",
    },
  ]);

  outputImage.png({ quality: 80 }).toFile(outputPath, (err, info) => {
    if (err) {
      throw new Error(`Failed to write output image: ${err}`);
    } else {
      console.log(`Output image saved to ${outputPath}`);
    }
  });
}

processImages();
