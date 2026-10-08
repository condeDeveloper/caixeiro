using Conde.Caixeiro;

var pedido = args.Length > 0 ? args[0] : "tudo";
var tudo = pedido == "tudo";

if (tudo || pedido == "definicao") Definicao();
if (tudo || pedido == "circulo") Circulos();
if (tudo || pedido == "razao") Razoes();
if (tudo || pedido == "cruzamento") Cruzamentos();
if (tudo || pedido == "partida") Partidas();
if (tudo || pedido == "grande") Grandes();
if (tudo || pedido == "custo") Custos();

static void Definicao()
{
    Console.WriteLine();
    Console.WriteLine("  AS HEURISTICAS CONTRA A DEFINICAO");
    Console.WriteLine();
    Console.WriteLine("  mapa                    cidades     otimo   na ordem   vizinho   insercao   com 2-opt   os dois exatos batem   triangular");

    foreach (var l in Medidas.ContraADefinicao())
        Console.WriteLine($"  {l.Mapa,-23} {l.Cidades,7} {l.Otimo,9:N0} {l.NaOrdem,10:N0} {l.Vizinho,9:N0} {l.Insercao,10:N0} {l.ComDoisOpt,11:N0} {Marca(l.OsDoisExatosBatem),22} {Marca(l.Triangular),12}");

    Console.WriteLine();
    Console.WriteLine("  os dois exatos percorrem espacos diferentes: um as ORDENS, o outro os");
    Console.WriteLine("  SUBCONJUNTOS de cidades ja visitadas");
    Console.WriteLine();
}

static void Circulos()
{
    Console.WriteLine();
    Console.WriteLine("  O CIRCULO, QUE EU MONTEI PARA ENGANAR E QUE NAO ENGANA NINGUEM");
    Console.WriteLine();
    Console.WriteLine("  cidades       otimo     vizinho    insercao   com 2-opt   vizinho sobre o otimo   com 2-opt sobre o otimo");

    foreach (var l in Medidas.NoCirculo())
        Console.WriteLine($"  {l.Cidades,7} {l.Otimo,11:N0} {l.Vizinho,11:N0} {l.Insercao,11:N0} {l.ComDoisOpt,11:N0} {l.RazaoVizinho,23:F2} {l.RazaoComDoisOpt,25:F2}");

    Console.WriteLine();
    Console.WriteLine("  andar pela borda E a resposta otima, e a ultima aresta fecha o circulo em vez");
    Console.WriteLine("  de atravessa-lo: as tres acertam em cheio, em todo tamanho");
    Console.WriteLine();
}

static void Razoes()
{
    Console.WriteLine();
    Console.WriteLine("  A RAZAO CONTRA O OTIMO, EM 60 MAPAS SORTEADOS NO PLANO DE CADA TAMANHO");
    Console.WriteLine();
    Console.WriteLine("  cidades   mapas   vizinho (media)   vizinho (pior)   insercao (media)   insercao (pior)   2-opt (media)   2-opt (pior)");

    foreach (var l in Medidas.Razoes())
        Console.WriteLine($"  {l.Cidades,7} {l.Mapas,7} {l.MediaVizinho,17:F3} {l.PiorVizinho,16:F3} {l.MediaInsercao,18:F3} {l.PiorInsercao,17:F3} {l.MediaComDoisOpt,15:F3} {l.PiorComDoisOpt,14:F3}");

    Console.WriteLine();
    Console.WriteLine("  a coluna do PIOR importa mais que a da media: uma heuristica usada de verdade");
    Console.WriteLine("  roda uma vez num mapa so");
    Console.WriteLine();
}

static void Cruzamentos()
{
    Console.WriteLine();
    Console.WriteLine("  A PROPRIEDADE DO 2-OPT: A ROTA DELE NAO TEM CRUZAMENTO");
    Console.WriteLine();
    Console.WriteLine("  mapa              cidades   vizinho cruza   insercao cruza   com 2-opt cruza");

    foreach (var l in Medidas.Cruzamentos())
        Console.WriteLine($"  {l.Mapa,-17} {l.Cidades,7} {Marca(l.VizinhoCruza),15} {Marca(l.InsercaoCruza),16} {Marca(l.ComDoisOptCruza),17}");

    Console.WriteLine();
    Console.WriteLine("  duas arestas que se cruzam podem sempre ser destrocadas com ganho: uma rota");
    Console.WriteLine("  de onde nao da para melhorar nao pode ter cruzamento");
    Console.WriteLine();
}

static void Partidas()
{
    Console.WriteLine();
    Console.WriteLine("  O VIZINHO MAIS PROXIMO PARTINDO DE CADA CIDADE, NO MESMO MAPA");
    Console.WriteLine();
    Console.WriteLine("  cidades   otimo   melhor partida   pior partida   melhor sobre o otimo   pior sobre o otimo");

    foreach (var l in Medidas.PorPartida())
        Console.WriteLine($"  {l.Partidas,7} {l.Otimo,7:N0} {l.Melhor,16:N0} {l.Pior,14:N0} {l.RazaoDoMelhor,22:F2} {l.RazaoDoPior,20:F2}");

    Console.WriteLine();
    Console.WriteLine("  a cidade de partida e uma escolha que ninguem faz com cuidado, e ela muda a");
    Console.WriteLine("  resposta");
    Console.WriteLine();
}

static void Grandes()
{
    Console.WriteLine();
    Console.WriteLine("  AS HEURISTICAS NOS MAPAS GRANDES, ONDE OS EXATOS NAO CHEGAM");
    Console.WriteLine();
    Console.WriteLine("  mapa              cidades      na ordem       vizinho      insercao     com 2-opt   o 2-opt tirou");

    foreach (var l in Medidas.Grandes())
        Console.WriteLine($"  {l.Mapa,-17} {l.Cidades,7:N0} {l.NaOrdem,13:N0} {l.Vizinho,13:N0} {l.Insercao,13:N0} {l.ComDoisOpt,13:N0} {l.GanhoDoDoisOpt,14:F1}%");

    Console.WriteLine();
    Console.WriteLine("  sem coluna de otimo aqui, de proposito: numero que nao foi conferido contra");
    Console.WriteLine("  a definicao nao entra em tabela nenhuma deste repositorio");
    Console.WriteLine();
}

static void Custos()
{
    Console.WriteLine();
    Console.WriteLine("  O CUSTO DOS DOIS METODOS EXATOS");
    Console.WriteLine();
    Console.WriteLine("  cidades        rotas que existem   estados de Held e Karp   quantas vezes menos");

    foreach (var l in Medidas.Custos())
        Console.WriteLine($"  {l.Cidades,7} {l.Rotas,24:N0} {l.EstadosDeHeldKarp,24:N0} {l.QuantasVezesMenos,21:N0}");

    Console.WriteLine();
    Console.WriteLine("  os dois sao exponenciais e os dois resolvem o mesmo problema: um cresce como");
    Console.WriteLine("  o fatorial e o outro como dois elevado a n vezes n ao quadrado");
    Console.WriteLine();
}

static string Marca(bool valor) => valor ? "sim" : "NAO";
