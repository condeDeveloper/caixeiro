# caixeiro

O problema do caixeiro viajante do zero em C# e .NET 8: três heurísticas, **dois**
métodos exatos que percorrem espaços completamente diferentes, e a definição no
papel de juiz — a melhor rota é a melhor, e saber que ela é a melhor exige olhar
todas as (n−1)! outras.

```
$ dotnet medidor.dll razao

  cidades   mapas   vizinho (media)   vizinho (pior)   insercao (media)   insercao (pior)   2-opt (media)   2-opt (pior)
        8      60             1.103            1.390              1.029             1.232           1.003          1.086
        9      60             1.106            1.354              1.031             1.210           1.002          1.028
       10      60             1.115            1.413              1.047             1.266           1.006          1.159
       11      60             1.128            1.410              1.053             1.178           1.009          1.120
```

O **vizinho mais próximo** erra 11% na média e até **41%** no pior mapa. A
**inserção mais barata** erra 5%. O **2-opt**, que não monta rota nenhuma e só
conserta uma pronta, erra menos de 1%.

A coluna do pior importa mais que a da média: uma heurística usada de verdade roda
uma vez num mapa só.

## O 2-opt não cruza, e isso é um teorema

O 2-opt pega dois pares de cidades vizinhas na rota, troca as ligações e inverte o
trecho do meio; se o custo caiu, fica com a troca. Ele repete até não haver mais o
que melhorar.

O que ele garante é fácil de enunciar e nada óbvio: num mapa que obedece à
desigualdade triangular, a rota que ele devolve **não tem cruzamento nenhum**.
Duas arestas que se cruzam podem sempre ser destrocadas com ganho, então uma rota
de onde não dá para melhorar não pode ter cruzamento.

```
$ dotnet medidor.dll cruzamento

  mapa              cidades   vizinho cruza   insercao cruza   com 2-opt cruza
  plano de 10            10             NAO              NAO               NAO
  plano de 20            20             NAO              NAO               NAO
  plano de 40            40             sim              sim               NAO
  plano de 80            80             sim              sim               NAO
```

É uma propriedade **geométrica**, conferida sobre os pontos do plano com um
determinante exato em inteiros, e não sobre a matriz de distâncias.

## Dois exatos que crescem por bases diferentes

```
$ dotnet medidor.dll custo

  cidades        rotas que existem   estados de Held e Karp   quantas vezes menos
        6                      120                      800                     0
       10                  362,880                   41,472                     9
       12               39,916,800                  247,808                   161
       15           87,178,291,200                3,211,264                27,148
       18      355,687,428,096,000               37,879,808             9,389,895
       20  121,645,100,408,832,000              189,267,968           642,713,618
```

A força bruta percorre as **ordens** de visita. **Held e Karp**, de 1962, percorre
pares de (conjunto de cidades já visitadas, cidade atual): o melhor caminho que
sai de zero, visita exatamente aquele conjunto e para naquela cidade não depende
de **como** ele chegou lá.

Os dois são exponenciais, e o problema continua difícil. Mas com vinte cidades a
diferença entre eles é a diferença entre 121 **quatrilhões** de rotas e 189
milhões de estados.

E com seis cidades a força bruta é a **mais barata** das duas: os estados só
compensam depois.

## As heurísticas nos mapas grandes

```
$ dotnet medidor.dll grande

  mapa              cidades      na ordem       vizinho      insercao     com 2-opt   o 2-opt tirou
  circulo de 200        200     2,510,222        62,800        62,800        62,800            0.0%
  grade 20x20           400           798           418           418           418            0.0%
  plano de 100          100        56,148        10,274         9,151         7,980           22.3%
  plano de 500          500     2,542,177       210,439       196,591       174,530           17.1%
  sorteado de 200       200       102,651         6,729         8,389         3,503           47.9%
```

No mapa sem geometria nenhuma, o 2-opt tira **quase metade** do custo. No círculo
e na grade ele não tem o que melhorar: as outras duas já acertaram.

Sem coluna de ótimo aqui, de propósito: colocar nesta tabela um número que não foi
conferido contra a definição seria exatamente o tipo de número que este
repositório existe para não publicar.

## Como rodar

```
dotnet test testes/Caixeiro.Testes/Caixeiro.Testes.csproj
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

Os comandos do medidor são `definicao`, `razao`, `circulo`, `cruzamento`,
`partida`, `grande`, `custo` e `tudo`.

## O que as medidas me corrigiram

**O círculo, que eu montei para enganar o vizinho mais próximo, não engana
ninguém.** A ideia era que ele andasse pela borda e atravessasse o diâmetro para
fechar a volta. Andar pela borda **é** a resposta ótima, e a última aresta fecha o
círculo em vez de atravessá-lo.

```
$ dotnet medidor.dll circulo

  cidades       otimo     vizinho    insercao   com 2-opt   vizinho sobre o otimo   com 2-opt sobre o otimo
       10      61,800      61,800      61,800      61,800                    1.00                      1.00
      200      62,800      62,800      62,800      62,800                    1.00                      1.00
```

As três acertam em cheio, de dez a duzentas cidades. A tabela ficou no repositório
porque um resultado negativo medido vale mais que uma intuição não medida, e porque
ela é o contraste do mapa que separa de verdade — o plano sorteado, que é o caso
realista e o menos vistoso.

**A cidade de partida muda o resultado, e ninguém escolhe ela com cuidado.**

```
$ dotnet medidor.dll partida

  cidades   otimo   melhor partida   pior partida   melhor sobre o otimo   pior sobre o otimo
        8     169              169            214                   1.00                 1.27
        9     261              262            347                   1.00                 1.33
       10     259              259            300                   1.00                 1.16
```

Entre a melhor e a pior partida há até **33%** de diferença, e a melhor partida
acerta o ótimo nesses mapas. O custo de testar todas é o custo de uma vezes o
número de cidades — e a maioria das implementações começa na cidade zero e nunca
pensa nisso de novo.

**A garantia da inserção depende de uma propriedade do mapa que nem todo mapa
tem.** "A inserção mais barata não passa de duas vezes o ótimo" vale em mapas que
obedecem à desigualdade triangular. Os mapas de distâncias sorteadas sem geometria
nenhuma, que são os que eu gerei primeiro, quebram a desigualdade em 18 de 20
casos — e um mapa de preços de passagem aérea quebra ela o tempo todo.

Publicar a garantia sem conferir a condição seria publicar uma promessa que o
mapa não sustenta, então a condição virou uma coluna da tabela.

**O otimo que o gerador anuncia precisava ser conferido.** O círculo e a grade são
construídos sabendo a resposta, e "sei a resposta porque eu a construí" é uma
afirmação sobre a construção, não sobre o mapa. Os dois são conferidos contra a
força bruta onde ela cabe, e contra Held e Karp onde ela não cabe — e ter dois
juízes com alcances diferentes é justamente o que permite conferir a grade de doze
cidades.

## Licença

MIT.
