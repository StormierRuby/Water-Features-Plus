# Simple Water Foam — 1.3.1: limpeza de controles

Esta versão remove quatro sliders suspensos do painel e acrescenta dois botões para comparar os modos de amplitude. O controlador de água e o Swell da versão 1.3.0, confirmado como funcionando, permanecem inalterados. O pacote 1.3.0 foi mantido separadamente.

**Este ZIP contém código-fonte, não uma DLL pronta.** Use o projeto que já funciona no seu Cities: Skylines II 1.6.4f1. A compilação e os testes aqui usaram substitutos das bibliotecas do jogo; a interface revisada da 1.3.1 ainda precisa ser conferida no CS2. O comportamento do Swell 1.3.0 foi confirmado por você.

## O que mudou nesta atualização

- **Removidos do painel:** Animation speed, Height scattering, Displacement scattering e Wave-tip lighting. Não haverá mais sliders cinza para esses recursos suspensos.
- **Volume lighting:** mantém Ambient scattering e Wave-body lighting, sujeitos à disponibilidade dos campos.
- **Test native amplitude controls / Testar amplitudes nativas:** desliga Swell e ativa os multiplicadores relativos, sem redefinir os valores dos sliders.
- **Resume saved swell / Retomar Swell salvo:** volta ao Swell usando seus valores atuais, sem aplicar o preset inicial.
- **30 sliders visíveis:** as quatro propriedades antigas ficam ocultas somente para preservar a configuração. Continuam sem escrever nos campos nativos.

### Investigar as amplitudes antigas

Os sliders Large waves multiplier e Medium waves multiplier não foram removidos. Ficam indisponíveis durante Swell porque os dois modos escrevem nas mesmas amplitudes nativas. Em **Ondas → Amplitudes nativas e vento fino**, use **Testar amplitudes nativas** para liberá-los.

Compare um de cada vez em **0%, 100% e 150%**, mantendo câmera e iluminação. Para isolar amplitudes, deixe Override ripple wind desligado. **100% mantém o valor original**, portanto apenas ativar o grupo pode quase não mudar a aparência. 150% equivale a 1,5 vezes a amplitude original; uma base baixa continua baixa e uma base zero continua zero.

Para voltar ao resultado anterior, clique em **Retomar Swell salvo**, no grupo Swell. Os botões preservam as configurações de aparência, o estado do ajuste de ripples e os valores de ambos os modos. A dependência antiga dos ripples em relação ao interruptor WavesEnabled não foi alterada nesta correção.

## Como funciona o Swell

O controle anterior de ondas grandes apenas multiplicava a amplitude que já existia. Não alterava a escala espacial do espectro; uma banda com amplitude original zero continuava em zero.

O novo grupo **Ondas longas / Swell** controla a região periódica da simulação, o vento visual que gera o espectro, a força das duas bandas e sua irregularidade. No HDRP upstream, superfícies oceânicas combinam duas bandas de swell com uma banda opcional de ripples. O objetivo é destacar ondulações amplas, mantendo ondas menores sobrepostas. Não foi criada uma quarta banda nem um shader de deslocamento próprio.

| Controle | Faixa | Preset inicial | Campo nativo |
| --- | --- | --- | --- |
| Escala do domínio oceânico | 250–5000 m | 1500 m | `repetitionSize` |
| Vento gerador da ondulação | 0–100 km/h | 45 km/h | `largeWindSpeed` |
| Força da banda longa | 0–100% | 85% | `largeBand0Multiplier` |
| Força da banda secundária | 0–100% | 40% | `largeBand1Multiplier` |
| Irregularidade da ondulação | 0–100 | 40 | `largeChaos` |

**1500 m é o tamanho do domínio periódico, não a distância entre duas cristas.** A escala e o vento modificam a distribuição das ondas; não existe aqui um slider de comprimento de onda exato. As duas bandas também não têm espectros totalmente independentes: o HDRP deriva seus tamanhos a partir do domínio principal e compartilha o vento gerador.

85% corresponde a um multiplicador nativo absoluto de 0,85. Não significa 85% da amplitude anterior nem uma altura em metros. Assim, o novo modo pode atribuir força a uma banda cujo multiplicador era zero. A altura efetivamente renderizada continua dependendo do espectro, das máscaras e do material do jogo.

## Instalar no projeto existente

1. Feche totalmente o jogo. Guarde o `Mod.cs` que já funciona **fora da pasta do projeto**.
2. Extraia este ZIP e abra no Visual Studio a solução/projeto que você já compilou com sucesso.
3. Pelo Explorador de Soluções, abra o `Mod.cs` desse projeto e substitua todo seu conteúdo pelo deste pacote.
4. Salve e use **Recompilar / Rebuild** no projeto. Preserve seu `.csproj` e as referências existentes.
5. Abra o jogo e carregue uma cidade com água. Na guia Geral ou no log, confirme **v1.3.1**.
6. Entre em **Opções → Simple Water Foam → Ondas → Ondas longas / Swell** e clique em **Preset: ondulação longa**. Em inglês: **Waves → Ocean swell → Preset: long swell**.
7. Retorne à cidade e compare com a mesma câmera e iluminação. Solte cada slider para aplicar a alteração.

O preset de swell preserva seus ajustes de cores, espuma, reflexos e os demais grupos. Ativa o efeito geral e o novo modo de ondas. Não ativa nem desativa o ajuste separado de vento dos ripples.

Compilar um arquivo avulso aberto do ZIP não atualiza o projeto. Não deixe uma segunda cópia de `Mod.cs` ou um backup `.cs` na pasta compilada. Esta versão não exige dependências novas, um AssetBundle ou recompilar um shader. Mantenha a referência `Colossal.Localization` que resolveu o CS0012 anteriormente.

## Como comparar as três escalas

Comece pelo preset de swell, sem alterar os outros grupos. Esses números são um ponto de partida experimental, não uma calibração visual do seu mapa.

1. **Identificar a banda longa:** mantenha a força longa em 85 e reduza temporariamente a secundária a 0. Observe a superfície a uma distância suficiente para enxergar ondulações amplas.
2. **Comparar amplitude:** alterne a força longa entre 0 e 85. Se não houver diferença, registre o diagnóstico antes de aumentar todos os valores.
3. **Repor detalhe médio:** traga a secundária para 25–40. Isso permite procurar uma hierarquia visual entre ondulação ampla e detalhe intermediário.
4. **Ajustar escala e energia:** compare domínio de 1000, 1500 e 2000 m; depois vento de 30, 45 e 60 km/h, um controle por vez. O efeito não é linear. Mudar o espectro pode causar uma transição visual abrupta enquanto ele é regenerado.
5. **Ajustar organização:** compare irregularidade 20 e 60. Valores menores favorecem a direção do vento que já existe; não há um novo seletor de direção nesta versão.

Os ripples mantêm o comportamento anterior. Se quiser reduzi-los, use a ativação separada de vento das ondulações finas na mesma guia. Esse controle ainda é relativo ao vento original e não modifica diretamente a amplitude dos ripples.

Mudar o vento gerador também pode alterar a espuma produzida, mesmo sem mexer nos sliders de espuma. Compare primeiro o movimento e depois reajuste a cobertura, se necessário.

## Disponibilidade e limites

O novo modo exige uma superfície classificada pela engine como `OceanSeaLake` ou `Ocean`, além dos campos de domínio, vento e ambas as amplitudes. `largeChaos` é opcional: se faltar, somente seu slider fica indisponível. O controlador não converte superfícies `River` ou `Pool` em oceano.

A classificação é da **superfície da engine**, não de regiões geográficas do mapa. Se o CS2 representar mar, lagos e trechos de rios em uma única superfície oceânica, o ajuste se aplica a toda essa superfície. Esta versão não possui uma máscara para selecionar apenas o mar.

A linha **Superfícies compatíveis com swell** informa a quantidade encontrada. Carregue uma cidade antes de testar; se mostrar zero, o modo fica indisponível. A existência dos campos permite aplicar os parâmetros, mas não comprova que o shader personalizado do CS2 os usa integralmente.

O mod não altera a simulação hidrológica, não implementa rebentação costeira e não adiciona balanço físico dos barcos. A forma renderizada depende do material e da geometria que o jogo fornece. Não houve medição de desempenho no CS2. A implementação reutiliza as bandas existentes; isso não equivale a uma garantia de custo de renderização idêntico.

## Interação com os controles anteriores

- Com swell ativo, os antigos sliders **Ondas grandes — multiplicador** e **Ondas médias — multiplicador** ficam cinza. Os controles novos passam a comandar esses mesmos campos nativos, evitando dois escritores concorrentes.
- Ao desligar swell, o domínio, o vento gerador e a irregularidade anteriores são restaurados. Se o antigo grupo de amplitude estiver ativado, seus multiplicadores relativos voltam a valer; caso contrário, as amplitudes nativas são restauradas.
- Enquanto swell estiver ativo, o modo antigo de amplitude também fica suspenso nas superfícies não oceânicas. Nelas o mod devolve as amplitudes anteriores.
- **Aplicar ajustes (comparação A/B)** desliga ou religa os grupos selecionados sem apagar os sliders. A restauração usa os valores capturados na ativação e preserva uma escrita externa mais recente quando consegue identificá-la.
- **Usar valores do jogo** desliga também o modo swell. Os presets **equilibrado** e **somente espuma** redefinem as configurações e deixam swell desligado; não são necessários para testar esta atualização.

As configurações continuam globais para o mod, no mesmo arquivo `ModsSettings/SimpleWaterFoam/FoamSettings`. Valores salvos anteriormente prevalecem nos controles existentes. Evite dois mods ajustando os mesmos campos ao mesmo tempo, pois os controladores ativos podem disputar os valores.

## Animação e controles retirados do painel

A correção da 1.2.1 permanece: o mod **não escreve em `timeMultiplier`**. Não captura nem reaplica uma pausa e não força velocidade positiva. O jogo continua responsável por pausar, retomar e mudar o ritmo da animação.

Os controles **Height scattering**, **Displacement scattering**, **Wave-tip lighting** e velocidade da animação foram retirados do painel e continuam sem escrita. Seus valores salvos são preservados em propriedades ocultas para compatibilidade. Os três primeiros foram suspensos após seu teste sem efeito visual; isso não prova ausência do recurso no HDRP em geral.

Há 30 sliders nas seis guias; os quatro suspensos não são mais exibidos. Disponibilidade depende do campo, da superfície e do grupo selecionado. Consulte `CONTROLES.md` para o mapeamento completo.

| Guia | Ajustes |
| --- | --- |
| Geral | Ativação A/B, presets gerais, estado e diagnóstico |
| Água | Absorção, refração, RGB e contribuições de luz |
| Reflexos | Suavidade próxima/distante e transição |
| Ondas | Amplitudes anteriores, vento dos ripples e novo grupo Swell com preset próprio |
| Espuma | Quantidade, independência do vento, rugosidade, textura e persistência |
| Cáusticas | Ativação, intensidade, mistura vertical e projeção |

## Diagnóstico

Arquivo esperado:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\SimpleWaterFoam.log
```

Identificador desta atualização:

```text
SimpleWaterFoam 1.3.1: native water controls
```

A versão do assembly ainda depende do seu `.csproj`; use essa linha para conferir o código efetivamente carregado.

Em **Geral → Registrar diagnóstico no log**, o mod registra a leitura atual e agenda mais duas amostras após 5 e aproximadamente 15 segundos. Retorne à cidade e deixe rodar por pelo menos 20 segundos.

| Entrada | O que informa |
| --- | --- |
| `API` | Campo encontrado, ausente ou suspenso |
| `BOUND` | Superfície, tipo e quantidade de campos disponíveis |
| `SWELL` | Quantidade de superfícies compatíveis e disponibilidade da API |
| `WAVE_STATE` | Tipo, geometria, domínio, vento, amplitudes e redução por distância da banda longa |
| `WAVE_OUTPUT` | Número de bandas, domínios e amplitudes enviados à simulação, se essas estruturas estiverem expostas |
| `OBSERVE` | Leitura dos campos suspensos, incluindo o relógio; não altera seus valores |
| `Disabled control` | Um campo falhou e foi isolado |
| `Controller stopped` | Falha mais ampla interrompeu os ajustes e tentou restaurar os valores |

Se `WAVE_OUTPUT` mostrar `not exposed` ou valores vazios, a estrutura interna não pôde ser lida; isso sozinho não significa que a simulação parou. Se o campo mudar em `WAVE_STATE` mas não houver diferença visual, a próxima investigação deve ser o caminho de renderização, as máscaras e o fade nativos. Não basta aumentar o limite dos sliders.

## Validação realizada

O `Mod.cs` foi compilado em C# com tipos substitutos e passou por **81 verificações automatizadas de lógica**: 51 do controlador existente e 30 do modo swell e da alternância entre modos. Houve também um teste separado de pausa, retomada e mudança de velocidade. Os testes cobrem ativação a partir de amplitude zero, alternância entre modos sem acumular multiplicadores, restauração, tipo de superfície, API ausente, limites e preservação dos demais ajustes.

Esses testes não usam as DLLs proprietárias do jogo. Não validam a interface e serialização reais, o espectro renderizado, o resultado visual ou o FPS. O Swell da versão 1.3.0 foi confirmado por você. A 1.3.1 preserva esse controlador; a limpeza e os novos botões precisam ser conferidos no jogo. Faça a primeira comparação sem outro mod escrevendo nos mesmos parâmetros.

Para voltar ao pacote 1.3.0, desligue swell e feche o jogo, restaure o `Mod.cs` anterior e recompile o mesmo projeto. Os dois modos usam a mesma identidade de configurações; os campos existentes mantêm seus nomes.

## Referências técnicas

- [Unity HDRP 14 — propriedades da água](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem-Properties.html): bandas, domínio, vento e amplitudes. É referência upstream, não prova de compatibilidade do binário do CS2.
- [WaterSurface.cs, branch 2022.3](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/WaterSurface.cs): definição dos campos nativos.
- [HDRenderPipeline.WaterSystem.cs](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/HDRenderPipeline.WaterSystem.cs): composição dos parâmetros do espectro e das bandas.
- [HDRenderPipeline.WaterSystem.Simulation.cs](https://github.com/Unity-Technologies/Graphics/blob/2022.3/staging/Packages/com.unity.render-pipelines.high-definition/Runtime/Water/HDRenderPipeline.WaterSystem.Simulation.cs): atualização da simulação e do relógio.

Referências usadas anteriormente para os exemplos de opções/modding: [WaterVisualTweaks](https://github.com/HX2003/WaterVisualTweaks), [TrafficToolEssentials](https://github.com/ZessonsDE/TrafficToolEssentials/blob/main/TrafficToolEssentials/Settings.cs), [AreaBucket](https://github.com/Cmyna/AreaBucket/blob/master/Setting.cs) e [CS2MultiplayerMod](https://github.com/Rollocraft/CS2MultiplayerMod/blob/master/CS2MultiplayerMod/Setting.cs). Elas não são dependências que você precise instalar ou referenciar no projeto.
