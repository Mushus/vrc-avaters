import { program, Command } from "commander";
import mustache from "mustache";
import fs from "fs";
import path from "path";

interface Options {
  templates: string;
}

mustache.escape = function (text: string) {
  return text;
};

program
  .name("README generator")
  .description("Generate custom README")
  .version("0.1.0")
  .option("-t, --templates <string>", "template directory path")
  .action(async ({ templates }: Options, { args }: Command) => {
    const [output] = args;

    const params = {};

    function readFile(name: string): string {
      return fs.readFileSync(path.join(templates, name), "utf8");
    }

    const readmeTemplate = readFile("README.txt");
    const specsTemplate = readFile("specs.txt");
    const historyTemplate = readFile("history.txt");

    const specs = mustache.render(specsTemplate, params);
    const history = mustache.render(historyTemplate, params);
    const readme = mustache.render(readmeTemplate, {
      ...params,
      specs,
      history,
    });

    fs.writeFileSync(output, readme);
  });

program.parse();
