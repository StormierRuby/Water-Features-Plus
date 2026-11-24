# Controles — Simple Water Foam 1.3.1

30 sliders visíveis, em seis guias. Height, Displacement, Tip e AnimationSpeed foram retirados da interface; suas propriedades salvas permanecem ocultas e não são aplicadas.

Na guia Ondas, Testar amplitudes nativas desliga Swell e libera Large/Medium waves multiplier. Retomar Swell salvo recupera o modo Swell sem redefinir valores. Os modos são alternativos e compartilham os mesmos campos nativos de amplitude.

Os valores abaixo são os valores iniciais do mod. Configurações já salvas prevalecem. Um campo detectado não garante a mesma resposta em qualquer material ou versão do jogo.

## Água

| Slider | Faixa | Passo | Inicial | Campo nativo |
| --- | --- | --- | --- | --- |
| Distância de absorção (m) | 0,5–100 | 0,5 | 8 | `absorptionDistance` |
| Distância máxima de refração (m) | 0,1–5 | 0,1 | 1 | `maxRefractionDistance` |
| Vermelho — transmissão | 0–100 | 1 | 6 | `refractionColor` |
| Verde — transmissão | 0–100 | 1 | 30 | `refractionColor` |
| Azul — transmissão | 0–100 | 1 | 40 | `refractionColor` |
| Vermelho — dispersão | 0–100 | 1 | 6 | `scatteringColor` |
| Verde — dispersão | 0–100 | 1 | 25 | `scatteringColor` |
| Azul — dispersão | 0–100 | 1 | 22 | `scatteringColor` |
| Luz ambiente | 0–100 | 1 | 10 | `ambientScattering` |
| Luz no corpo das ondas | 0–100 | 1 | 40 | `directLightBodyScattering` |

- **Distância de absorção (m):** Valores maiores permitem enxergar mais longe através da água; depende da iluminação e do fundo.
- **Distância máxima de refração (m):** Limita a profundidade usada na distorção da refração. Não é a distância de desenho do cenário.
- **Vermelho — transmissão:** Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.
- **Verde — transmissão:** Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.
- **Azul — transmissão:** Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.
- **Vermelho — dispersão:** Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.
- **Verde — dispersão:** Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.
- **Azul — dispersão:** Componente nativo normalizado: 0–100 corresponde a 0–1. Não representa uma amostra sRGB/hex da imagem final.
- **Luz ambiente:** Escala de 0–100 para o parâmetro nativo de 0–1. Não é exposição global.
- **Luz no corpo das ondas:** Escala de 0–100 para o parâmetro nativo de 0–1. Não é exposição global.

## Reflexos

| Slider | Faixa | Passo | Inicial | Campo nativo |
| --- | --- | --- | --- | --- |
| Suavidade próxima | 0–100 | 1 | 85 | `startSmoothness` |
| Suavidade distante | 0–100 | 1 | 80 | `endSmoothness` |
| Início da transição (m) | 0–3000 | 25 | 100 | `smoothnessFadeStart` |
| Comprimento da transição (m) | 25–4000 | 25 | 500 | `smoothnessFadeDistance` |

- **Suavidade próxima:** Mais alto concentra o brilho; mais baixo aumenta a rugosidade. Não equivale à intensidade do reflexo.
- **Suavidade distante:** Controla a suavidade após a transição por distância.
- **Início da transição (m):** Distância da câmera onde começa a mudança de suavidade.
- **Comprimento da transição (m):** Comprimento da transição, contado a partir de seu início.

## Ondas

| Slider | Faixa | Passo | Inicial | Campo nativo |
| --- | --- | --- | --- | --- |
| Ondas grandes — multiplicador (%) | 0–150 | 5 | 100 | `largeBand0Multiplier` |
| Ondas médias — multiplicador (%) | 0–150 | 5 | 100 | `largeBand1Multiplier` |
| Agitação fina — multiplicador (%) | 0–150 | 5 | 100 | `ripplesWindSpeed` |
| Escala do domínio oceânico (m) | 250–5000 | 250 | 1500 | `repetitionSize` |
| Vento gerador da ondulação (km/h) | 0–100 | 1 | 45 | `largeWindSpeed` |
| Força da banda longa (%) | 0–100 | 1 | 85 | `largeBand0Multiplier` |
| Força da banda secundária (%) | 0–100 | 1 | 40 | `largeBand1Multiplier` |
| Irregularidade da ondulação | 0–100 | 1 | 40 | `largeChaos` |

- **Ondas grandes — multiplicador (%):** Multiplica a amplitude original: 0% remove a contribuição da banda, 100% mantém e 150% aumenta pela metade. Zero original continua zero. Swell ativo controla este mesmo campo; use Testar amplitudes nativas para liberar este slider.
- **Ondas médias — multiplicador (%):** Multiplica a amplitude original: 0% remove a contribuição da banda, 100% mantém e 150% aumenta pela metade. Zero original continua zero. Swell ativo controla este mesmo campo; use Testar amplitudes nativas para liberar este slider.
- **Agitação fina — multiplicador (%):** 100 mantém o valor capturado ao ativar. A agitação fina multiplica o vento da banda de ripples, não diretamente sua amplitude. Se o valor original for zero, o resultado permanece zero.
- **Escala do domínio oceânico (m):** Tamanho da região periódica da simulação. Não é a distância exata entre cristas; também influencia a segunda banda.
- **Vento gerador da ondulação (km/h):** Altera a distribuição de energia, forma e amplitude das bandas. É um parâmetro visual da água, não o vento climático da cidade.
- **Força da banda longa (%):** Multiplicador absoluto de 0 a 1 da banda longa. Pode reativar uma banda cujo multiplicador original era zero. Não representa metros de altura.
- **Força da banda secundária (%):** Multiplicador absoluto de 0 a 1. Use valores menores que a banda longa para destacar a ondulação ampla, mantendo detalhe sobreposto.
- **Irregularidade da ondulação:** Valores menores favorecem a direção nativa do vento; maiores tornam o espectro menos direcional. Não escolhe uma nova direção.

## Espuma

| Slider | Faixa | Passo | Inicial | Campo nativo |
| --- | --- | --- | --- | --- |
| Quantidade de espuma | 0–100 | 1 | 40 | `simulationFoamAmount` |
| Independência do vento | 0–100 | 1 | 100 | `windFoamCurve / simulationFoamWindCurve` |
| Rugosidade da espuma | 0–100 | 1 | 65 | `simulationFoamSmoothness / foamSmoothness` |
| Repetição da textura (por metro) | 0,1–2 | 0,1 | 0,2 | `foamTextureTiling` |
| Persistência (escala nativa) | 0–100 | 1 | 20 | `simulationFoamDrag` |

- **Quantidade de espuma:** Altera a quantidade/tamanho das manchas. Não é um controle de opacidade.
- **Independência do vento:** 0 preserva a curva nativa; 100 remove sua atenuação. Não cria ondas nem altera o vento do mapa.
- **Rugosidade da espuma:** Mais alto torna a resposta especular mais difusa. Internamente aplica 1 menos a rugosidade à suavidade nativa.
- **Repetição da textura (por metro):** Mais alto repete a textura mais vezes e reduz o tamanho do detalhe. Não altera diretamente a cobertura.
- **Persistência (escala nativa):** 0–100 mapeia o parâmetro para 0–1. Teste gradualmente; depende do suporte do shader e da simulação de espuma.

## Cáusticas

| Slider | Faixa | Passo | Inicial | Campo nativo |
| --- | --- | --- | --- | --- |
| Intensidade das cáusticas | 0–100 | 1 | 30 | `causticsIntensity` |
| Transição vertical (m) | 0,1–10 | 0,1 | 1 | `causticsPlaneBlendDistance` |
| Distância de projeção (m) | 0,5–15 | 0,5 | 5 | `virtualPlaneDistance` |

- **Intensidade das cáusticas:** Comece com valores baixos para evitar faixas muito claras ou ciano junto às margens.
- **Transição vertical (m):** Distância de mistura vertical das cáusticas.
- **Distância de projeção (m):** Valores maiores podem produzir padrões mais nítidos e também artefatos.

## Notas de aplicação

No modo nativo, 100% conserva a amplitude original, 0% remove a contribuição da banda e 150% multiplica a base por 1,5. Uma base zero continua zero. Durante Swell os multiplicadores relativos ficam indisponíveis; ajuste Long-band strength e Secondary-band strength ou use o botão para trocar de modo.

O Swell preserva a implementação 1.3.0. Seus cinco valores, as regras de superfície OceanSeaLake/Ocean, os limites e a restauração não foram alterados. Nenhum modo escreve no relógio da água.

Os quatro campos antigos continuam sendo lidos no diagnóstico, sem aparecer como sliders. Dos 31 bindings internos, quatro são somente de observação. A contagem de bindings não equivale à quantidade de sliders.
