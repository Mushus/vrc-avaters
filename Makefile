curdir = $(shell pwd)
node = ${curdir}/node_modules/.bin/ts-node

include .env

.PHONY: import, build, clean, bundle, round

round: # 角丸画像が欲しかった
	yarn ts-node scripts\roundedImage.ts

import:
	mkdir -p ${curdir}/src
	rm -rf ${curdir}/src/*
	GOOGLE_APPLICATION_CREDENTIALS=${curdir}/credentials.json \
	${node} -T ${curdir}/scripts/importer.ts \
		--exclude '*.blend1' \
		--output ${curdir}/src \
		--file-id ${SRC_GDRIVE_FILE_ID}

build: dist/Avater.unitypackage

bundle: bundle.zip

clean:
	rm -rf ./dist ./tmp bundle.zip

dist:
	mkdir -p ./dist

dist/src: dist
	cp -r ./src ./dist/src

dist/Avater.unitypackage: dist tmp/Assets/${AVATER_ASSET_DIR} tmp/Assets/${AVATER_ASSET_DIR}.meta
	cd tmp && \
	${node} -T \
		${curdir}/scripts/unitypacker.ts \
		--output ${curdir}/dist/Avater.unitypackage \
		--recursive \
		./Assets/${AVATER_ASSET_DIR}

dist/README.txt: dist
	cp templates/README.txt dist/README.txt

bundle.zip: dist/README.txt dist/Avater.unitypackage dist/src
	cd dist && zip -r ${curdir}/bundle.zip .

tmp/Assets:
	mkdir -p ./tmp/Assets

tmp/Assets/${AVATER_ASSET_DIR}.meta: tmp/Assets
	cp -r ./unity/Assets/${AVATER_ASSET_DIR}.meta ./tmp/Assets

tmp/Assets/${AVATER_ASSET_DIR}: tmp/Assets
	cp -r ./unity/Assets/${AVATER_ASSET_DIR} ./tmp/Assets
	${node} -T ${curdir}/scripts/avaterIdRemover.ts \
		${curdir}/tmp/Assets/${AVATER_ASSET_DIR}

