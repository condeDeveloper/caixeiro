namespace Conde.Caixeiro;

/// <summary>
/// As TABELAS que o medidor imprime e que os testes conferem.
///
/// Elas ficam aqui, e nao dentro do programa do medidor, por um motivo so: numero
/// em README que ninguem confere envelhece errado em silencio. Alguem troca uma
/// constante, a tabela muda, e o texto continua afirmando a frase antiga. Aqui o
/// teste afirma sobre o MESMO objeto que vira linha impressa.
/// </summary>
public static class Medidas
{
    public record LinhaDefinicao(
        string Mapa, int Cidades, long Otimo, long NaOrdem, long Vizinho, long Insercao, long ComDoisOpt,
        bool OsDoisExatosBatem, bool Triangular);

    /// <summary>As heuristicas contra a DEFINICAO, nos mapas em que ela cabe.</summary>
    public static List<LinhaDefinicao> ContraADefinicao(int sorteados = 8)
    {
        var linhas = new List<LinhaDefinicao>();

        foreach (var (nome, mapa) in Mapas.Pequenos(sorteados))
        {
            var forca = ForcaBruta.Melhor(mapa);
            var heldKarp = ForcaBruta.HeldKarp(mapa);

            linhas.Add(new LinhaDefinicao(
                nome, mapa.Cidades, forca.Custo,
                Heuristicas.NaOrdem(mapa).Custo,
                Heuristicas.VizinhoMaisProximo(mapa).Custo,
                Heuristicas.InsercaoMaisBarata(mapa).Custo,
                Heuristicas.VizinhoMais2Opt(mapa).Custo,
                forca.Custo == heldKarp.Custo, mapa.ObedeceATriangular()));
        }

        return linhas;
    }

    public record LinhaCusto(int Cidades, long Rotas, long OlhadasDaForcaBruta, long EstadosDeHeldKarp, double QuantasVezesMenos);

    /// <summary>
    /// O custo dos DOIS metodos exatos, em rotas e em estados.
    ///
    /// Os dois sao exponenciais e os dois resolvem o mesmo problema. A diferenca e
    /// que um cresce como o fatorial e o outro como dois elevado a n vezes n ao
    /// quadrado, e com vinte cidades isso e a diferenca entre o impossivel e o
    /// demorado.
    /// </summary>
    public static List<LinhaCusto> Custos()
    {
        var linhas = new List<LinhaCusto>();

        foreach (var cidades in new[] { 6, 8, 10, 12, 15, 18, 20 })
        {
            var rotas = ForcaBruta.Fatorial(cidades - 1);
            var estados = (long)(1 << (cidades - 1)) * (cidades - 1) * (cidades - 1);

            linhas.Add(new LinhaCusto(cidades, rotas, rotas, estados, (double)rotas / estados));
        }

        return linhas;
    }

    public record LinhaCirculo(int Cidades, long Otimo, long Vizinho, long Insercao, long ComDoisOpt, double RazaoVizinho, double RazaoComDoisOpt);

    /// <summary>
    /// O CIRCULO, que eu montei achando que enganaria o vizinho mais proximo, e que
    /// NAO ENGANA.
    ///
    /// A ideia era que ele andasse pela borda e atravessasse o diametro para fechar.
    /// Andar pela borda E a resposta otima, e a ultima aresta fecha o circulo em vez
    /// de atravessa-lo: as tres heuristicas acertam em cheio, em todo tamanho.
    ///
    /// A tabela ficou porque um resultado negativo medido vale mais que uma
    /// intuicao nao medida, e porque ela e o contraste do mapa que SEPARA de
    /// verdade, que e o plano sorteado.
    /// </summary>
    public static List<LinhaCirculo> NoCirculo()
    {
        var linhas = new List<LinhaCirculo>();

        foreach (var cidades in new[] { 10, 20, 50, 100, 200 })
        {
            var (mapa, otimo) = Mapas.NoCirculo(cidades, 10_000, 7);

            var vizinho = Heuristicas.VizinhoMaisProximo(mapa).Custo;
            var insercao = Heuristicas.InsercaoMaisBarata(mapa).Custo;
            var comDoisOpt = Heuristicas.VizinhoMais2Opt(mapa).Custo;

            linhas.Add(new LinhaCirculo(
                cidades, otimo, vizinho, insercao, comDoisOpt,
                (double)vizinho / otimo, (double)comDoisOpt / otimo));
        }

        return linhas;
    }

    public record LinhaRazao(int Cidades, int Mapas, double MediaVizinho, double PiorVizinho, double MediaInsercao, double PiorInsercao, double MediaComDoisOpt, double PiorComDoisOpt);

    /// <summary>
    /// A RAZAO contra o otimo em muitos mapas sorteados no plano, onde a forca bruta
    /// ainda chega.
    ///
    /// E aqui que os metodos se separam. O circulo nao separa nada e a grade quase
    /// nada; o plano sorteado, que e o caso realista, separa os tres por margens que
    /// nao sao pequenas.
    ///
    /// A coluna do PIOR importa mais que a da media: uma heuristica usada de verdade
    /// roda uma vez num mapa so, e o que ela entrega naquele mapa pode ser o pior
    /// caso dela.
    /// </summary>
    public static List<LinhaRazao> Razoes(int mapas = 60)
    {
        var linhas = new List<LinhaRazao>();

        foreach (var cidades in new[] { 8, 9, 10, 11 })
        {
            var vizinho = new List<double>();
            var insercao = new List<double>();
            var comDoisOpt = new List<double>();

            for (var k = 1UL; k <= (ulong)mapas; k++)
            {
                var (mapa, _) = Mapas.NoPlano(cidades, 1_000, k);
                var otimo = (double)ForcaBruta.Melhor(mapa).Custo;

                if (otimo == 0) continue;

                vizinho.Add(Heuristicas.VizinhoMaisProximo(mapa).Custo / otimo);
                insercao.Add(Heuristicas.InsercaoMaisBarata(mapa).Custo / otimo);
                comDoisOpt.Add(Heuristicas.VizinhoMais2Opt(mapa).Custo / otimo);
            }

            linhas.Add(new LinhaRazao(
                cidades, mapas,
                vizinho.Average(), vizinho.Max(),
                insercao.Average(), insercao.Max(),
                comDoisOpt.Average(), comDoisOpt.Max()));
        }

        return linhas;
    }

    public record LinhaGrande(
        string Mapa, int Cidades, long NaOrdem, long Vizinho, long Insercao, long ComDoisOpt, double GanhoDoDoisOpt);

    /// <summary>
    /// As heuristicas nos mapas grandes, onde os exatos nao chegam.
    ///
    /// Sem coluna de otimo aqui, de proposito: colocar nesta tabela um numero que
    /// nao foi conferido contra a definicao seria exatamente o tipo de numero que
    /// este repositorio existe para nao publicar.
    /// </summary>
    public static List<LinhaGrande> Grandes()
    {
        var linhas = new List<LinhaGrande>();

        foreach (var (nome, mapa) in Mapas.Grandes())
        {
            var vizinho = Heuristicas.VizinhoMaisProximo(mapa).Custo;
            var comDoisOpt = Heuristicas.VizinhoMais2Opt(mapa).Custo;

            linhas.Add(new LinhaGrande(
                nome, mapa.Cidades,
                Heuristicas.NaOrdem(mapa).Custo, vizinho,
                Heuristicas.InsercaoMaisBarata(mapa).Custo, comDoisOpt,
                100.0 * (vizinho - comDoisOpt) / vizinho));
        }

        return linhas;
    }

    public record LinhaCruzamento(string Mapa, int Cidades, bool VizinhoCruza, bool InsercaoCruza, bool ComDoisOptCruza);

    /// <summary>
    /// A propriedade do 2-OPT: a rota que ele devolve NAO TEM CRUZAMENTO.
    ///
    /// Duas arestas que se cruzam podem sempre ser destrocadas com ganho num mapa
    /// que obedece a desigualdade triangular, entao uma rota de onde nao da para
    /// melhorar nao pode ter cruzamento. E uma propriedade GEOMETRICA conferida
    /// sobre os pontos, e nao sobre a matriz de distancias.
    /// </summary>
    public static List<LinhaCruzamento> Cruzamentos()
    {
        var linhas = new List<LinhaCruzamento>();

        foreach (var cidades in new[] { 10, 20, 40, 80 })
        {
            var (mapa, pontos) = Mapas.NoPlano(cidades, 1_000, (ulong)cidades);

            linhas.Add(new LinhaCruzamento(
                $"plano de {cidades}", cidades,
                Heuristicas.TemCruzamento(pontos, Heuristicas.VizinhoMaisProximo(mapa).Rota),
                Heuristicas.TemCruzamento(pontos, Heuristicas.InsercaoMaisBarata(mapa).Rota),
                Heuristicas.TemCruzamento(pontos, Heuristicas.VizinhoMais2Opt(mapa).Rota)));
        }

        return linhas;
    }

    public record LinhaPartida(int Partidas, long Melhor, long Pior, long Otimo, double RazaoDoMelhor, double RazaoDoPior);

    /// <summary>
    /// O vizinho mais proximo partindo de CADA cidade, no mesmo mapa.
    ///
    /// E a medida que diz o quanto ele depende de uma escolha que ninguem faz com
    /// cuidado: a cidade de partida. Entre a melhor e a pior partida ha uma
    /// diferenca que nao e pequena, e o custo de testar todas e o custo de uma
    /// vezes n.
    /// </summary>
    public static List<LinhaPartida> PorPartida()
    {
        var linhas = new List<LinhaPartida>();

        foreach (var cidades in new[] { 8, 9, 10, 11 })
        {
            var (mapa, _) = Mapas.NoPlano(cidades, 100, (ulong)(cidades * 7));
            var otimo = ForcaBruta.Melhor(mapa).Custo;
            var custos = new List<long>();

            for (var c = 0; c < cidades; c++) custos.Add(Heuristicas.VizinhoMaisProximo(mapa, c).Custo);

            linhas.Add(new LinhaPartida(
                cidades, custos.Min(), custos.Max(), otimo,
                (double)custos.Min() / otimo, (double)custos.Max() / otimo));
        }

        return linhas;
    }
}
