let humanCount = 100;
let foodCount = humanCount * 100;
let moveDistance = 0;

for (let i = 1; 0 < foodCount; i++) {
    const optimalHumanCount = Math.floor(foodCount / 100) + 1;
    if (optimalHumanCount < humanCount) {
        humanCount = optimalHumanCount;
    }

    foodCount -= humanCount;
    moveDistance++;
    console.log(
        `${i}日目: 人数:${humanCount} 食料:${foodCount} 移動距離:${moveDistance}`
    );
}

console.log(`移動距離${moveDistance}`);
