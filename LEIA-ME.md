# Water Surface Controls

Ajuste a espuma, combine ondas longas e médias e controle como a água de **Cities: Skylines II** responde à luz. O mod usa a superfície de água nativa do jogo. Os ajustes são visuais e se aplicam às cidades que você carregar.

**Versão do mod** 1.4.1 · **Versão-alvo do jogo** 1.6.4f1 · **Interface** Português do Brasil e inglês

## Primeiros ajustes

1. Carregue uma cidade e posicione a câmera de frente para um trecho de água aberta. Abra **Opções → Water Surface Controls**.
2. Na guia **Espuma**, mova **Quantidade de espuma** e solte o controle para aplicar. Em um perfil novo, somente a espuma vem ajustada, com quantidade **40** e independência do vento **100**.
3. Para experimentar ondas maiores, abra **Ondas → Ondas longas / Swell** e clique em **Preset: ondulação longa**. O preset ajusta as duas bandas, o domínio, o vento e a irregularidade. Os ajustes de espuma, cor e reflexos são preservados.
4. Para comparar o resultado no mesmo enquadramento, desligue e religue **Geral → Aplicar ajustes (comparação A/B)**. Os valores dos sliders ficam salvos.

## Controles por guia

O painel tem **32 sliders em seis guias**, além de seletores, interruptores e presets. A disponibilidade depende do grupo ativado, da superfície carregada e dos campos que a versão do jogo expõe.

| Guia | O que você pode ajustar |
| --- | --- |
| **Geral** | Ligar os ajustes, comparar com a água original, aplicar presets e registrar diagnóstico. |
| **Água** | Distância de absorção, distância máxima de refração, cores RGB de transmissão e dispersão, luz ambiente e luz no corpo das ondas. Aumente a distância de absorção para enxergar mais fundo onde o leito está visível. |
| **Reflexos** | Suavidade próxima e distante, início e comprimento da transição entre elas. Uma suavidade maior concentra o brilho da superfície. |
| **Ondas** | Força das duas bandas, escala, vento e direção do Swell, multiplicadores nativos de amplitude, vento das ondulações finas e perda de detalhe com a distância da câmera. |
| **Espuma** | Quantidade, independência do vento, rugosidade e repetição da textura. A quantidade controla a cobertura das manchas. Aumente a repetição para obter detalhes menores dentro delas. |
| **Cáusticas** | Ativação, intensidade, transição vertical, distância de projeção e banda que forma os padrões de luz no fundo. Compare os resultados em água rasa, com o leito visível. |

### Ondas longas, médias e finas

O **Swell** permite regular a força das duas bandas separadamente. A banda longa produz ondulações amplas. A secundária acrescenta detalhe de escala média sobre elas. Os *ripples* são as ondulações mais finas.

| Controle de Swell | Faixa | Valor inicial |
| --- | --- | --- |
| Escala do domínio oceânico | 250–5.000 m | 1.500 m |
| Vento gerador da ondulação | 0–100 km/h | 45 km/h |
| Força da banda longa | 0–200% | 85% |
| Força da banda secundária | 0–200% | 40% |
| Irregularidade da ondulação | 0–100 | 40 |
| Direção do Swell | 0–360° em relação ao eixo X do mapa | Direção do jogo. O slider começa em 0° e atua ao ligar **Ajustar direção do Swell**. |

Para reconhecer cada escala, aplique o preset e leve a banda secundária a **0%**. Alterne a longa entre **0% e 100%**. Em seguida, mantenha a longa em **85%** e traga a secundária de volta a **40%**. Para observar a direção, diminua a irregularidade e compare **0° e 90°**, usando um cais como referência.

A escala do domínio define o tamanho da região em que o padrão de ondas se repete. A distância entre cristas e a altura visível dependem também do vento e da combinação das bandas. No Swell, **100%** atribui multiplicador **1,0** à banda e pode reativar uma banda com força original zero.

**Usar amplitudes nativas** troca para o outro modo. Nele, os controles de ondas grandes e médias multiplicam a amplitude original de cada superfície. **100%** conserva essa amplitude, **150%** aplica 1,5 vez o valor original e uma base zero continua em zero. **Retomar Swell salvo** recupera seus ajustes de Swell sem reaplicar o preset.

O Swell exige uma superfície identificada como `OceanSeaLake` ou `Ocean` e os campos necessários do espectro. Confira **Superfícies compatíveis com swell** na guia Ondas. Uma mesma superfície pode abranger várias regiões do mapa. Piscinas ficam fora dos ajustes do mod.

### Ripples por distância e cobertura da espuma

Em **Ondas → Ondulações finas por distância**, ligue **Ajustar visibilidade dos ripples**. **Início do fade** aceita 0–5.000 m e **Comprimento do fade** aceita 5–10.000 m. Com os valores iniciais de **50 m** e **200 m**, a transição vai de **50 m a 250 m da câmera**. Para comparar, observe um mesmo trecho a cerca de 100 m e alterne o início entre **0 m e 1.000 m**, com comprimento de **5 m**. O que se procura é a variação no detalhe fino da superfície.

Esse ajuste depende de ripples ativos e funciona independentemente do ajuste de vento. Para mudar o vento dos ripples, ative **Ajustar amplitude das ondas** e **Ajustar vento das ondulações finas (experimental)**. O multiplicador de agitação fina usa o vento original como base.

A cobertura das cristas brancas é controlada em **Espuma**. **Independência do vento** em **0** preserva a resposta nativa ao vento. Em **100**, remove a atenuação dessa curva. A persistência temporal da espuma ficou oculta na 1.4.1 porque seu efeito visual não foi confirmado. Esperar a simulação avançar, com a câmera parada, não testa o fade por distância.

### Cáusticas

Ligue **Ajustar cáusticas** e **Cáusticas ligadas**. Em uma superfície oceânica, **Banda das cáusticas** permite escolher **Longa**, **Secundária** ou **Ripples** para formar o padrão de luz. A opção **Jogo** devolve a escolha à superfície. Se a banda selecionada estiver inativa, o mod libera esse ajuste.

## Presets e restauração

- **Aplicar ajustes (comparação A/B)** alterna entre os grupos escolhidos e os valores que o mod encontrou ao ativá-los. Use esse interruptor para comparar sem perder o perfil.
- **Usar valores do jogo** desativa os grupos e mantém os valores dos sliders para uso posterior.
- **Somente espuma (padrão)** redefine os sliders e ativa apenas o ajuste de espuma.
- **Preset inicial equilibrado** também redefine os sliders. Ativa óptica, cores, suavidade, amplitudes nativas e aparência da espuma, com quantidade e independência do vento em **30**.
- **Preset: ondulação longa** aplica os cinco valores de Swell da tabela relativos ao domínio, vento, duas bandas e irregularidade. **Retomar Swell salvo** é a opção para voltar aos valores que você personalizou.

## Compilar e atualizar no Windows

1. Com a toolchain oficial de modding instalada, feche o jogo e abra **WaterSurfaceControls.sln** no Visual Studio.
2. Selecione **Release** e execute **Build → Rebuild Solution**. O ponto de entrada fica em `WaterSurfaceControls/Mod.cs` e os demais arquivos de execução, configurações e idioma ficam em `WaterSurfaceControls/src/`. A solução inclui a referência a `Colossal.Localization`.
3. Na instalação padrão, a compilação local implanta a DLL em `%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\WaterSurfaceControls\WaterSurfaceControls.dll`. Abra o jogo e confira **Opções → Water Surface Controls**.

Se você usava o **Simple Water Foam**, mova a instalação local antiga de `Mods\SimpleWaterFoam` para fora de `Mods` antes de iniciar a nova. Mantenha as preferências em `ModsSettings/SimpleWaterFoam/FoamSettings`. O nome antigo nesse caminho é intencional e permite reaproveitar os valores de espuma, Swell e dos demais grupos.

## Diagnóstico

O carregamento desta versão aparece como `WaterSurfaceControls 1.4.1 loaded` no log do mod.

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\WaterSurfaceControls.log
```

Para investigar um controle, selecione **Geral → Registrar diagnóstico no log** e volte à cidade por cerca de 20 segundos. O mod registra uma leitura imediata e outras duas após aproximadamente **5 e 15 segundos**.

| Entrada | O que conferir |
| --- | --- |
| `BOUND` e `SWELL` | Superfícies descobertas e disponibilidade dos campos necessários ao Swell. |
| `API` | Campos disponíveis, ausentes ou suspensos. |
| `WAVE_STATE` | Domínio, vento, força das bandas, direção, estado dos ripples, distâncias de fade e banda das cáusticas. |
| `WAVE_OUTPUT` | Bandas ativas, escalas e multiplicadores que o runtime permite consultar. |
| `OBSERVE` | Leitura dos campos suspensos, incluindo persistência, dispersão por altura e deslocamento, luz nas cristas e relógio da animação. O jogo controla a pausa, a retomada e a velocidade. |

Um valor diferente no log confirma a alteração do parâmetro. Para avaliar o efeito do material, compare imagens com a mesma câmera e iluminação, mudando um controle por vez. Ao relatar um problema, inclua a versão do jogo, os grupos ativos e as entradas de diagnóstico relevantes.

## Validação e referências

A versão 1.4.1 passou em **100 verificações automatizadas** com tipos substitutos de Unity e CS2, além de um teste de pausa e retomada. A suíte cobre, entre outros pontos, limites dos controles, troca de bandas, restauração dos valores e localização da interface. A aparência e o desempenho no jogo precisam ser avaliados em uma cidade carregada.

A implementação usa o HDRP como referência para interpretar os campos da água. O material fornecido pelo CS2 determina o resultado renderizado. [Propriedades da água no HDRP 14](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem-Properties.html) · [WaterSurface.cs](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSurface.cs) · [Sistema de água](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/HDRenderPipeline.WaterSystem.cs) · [Atualização da simulação](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/HDRenderPipeline.WaterSystem.Simulation.cs)

Os projetos consultados para exemplos de água e de interface são referências de desenvolvimento. A instalação do mod dispensa esses projetos. [WaterVisualTweaks](https://github.com/HX2003/WaterVisualTweaks) · [TrafficToolEssentials](https://github.com/ZessonsDE/TrafficToolEssentials/blob/main/TrafficToolEssentials/Settings.cs) · [AreaBucket](https://github.com/Cmyna/AreaBucket/blob/master/Setting.cs) · [CS2MultiplayerMod](https://github.com/Rollocraft/CS2MultiplayerMod/blob/master/CS2MultiplayerMod/Setting.cs)
