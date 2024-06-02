# Unity Compute Shaders

Uso de GPU para realizar cálculos de propósito general con Unity.

## Simulación de arena (2D)

Se han implementado 2 kernels para simular el comportamiento de la arena y extraer datos asociada a esta.

1. **Kernel de simulación**: necesario para pintar los píxeles con su color correspondiente, calcular el movimiento de la arena y por ello el recoloreado de los píxeles de los instantes anteriores.

2. **Kernel de extracción de datos**: se halla el número de granos de arena en pantalla además de la proporción de obstáculos pintada hasta el momento.

### Preview

https://github.com/pablomorillas02/sand-simulation/assets/114686516/1ab7f81c-a43b-47db-bca3-a4689b0e00c2
