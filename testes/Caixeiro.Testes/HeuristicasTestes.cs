using Conde.Caixeiro;
using Xunit;

namespace Conde.Caixeiro.Testes;

public class HeuristicasTestes
{
    /// <summary>
    /// Toda heuristica devolve uma ROTA VALIDA: uma permutacao de todas as cidades.
    ///
    /// E a unica coisa que todas elas prometem, e e preciso conferir porque duas
    /// delas montam a rota por insercao, e uma insercao com indice errado duplica
    /// uma cidade e perde outra sem que o resultado deixe de parecer plausivel.
    /// </summary>
    [Fact]
    public void TodaHeuristicaDevolveUmaRotaValida()
    {
        foreach (var (nome, mapa) in Mapas.Pequenos(10).Concat(Mapas.Grandes()))
            foreach (var (heuristica, rodar) in Heuristicas.Todas())
            {
                var r = rodar(mapa);

                Assert.True(mapa.EhRotaValida(r.Rota), $"{nome} com {heuristica}");
                Assert.Equal(r.Custo, mapa.Custo(r.Rota));
            }
    }

    /// <summary>
    /// E nenhuma fica ABAIXO do otimo, que seria a unica maneira de o juiz estar
    /// errado.
    /// </summary>
    [Fact]
    public void NenhumaFicaAbaixoDoOtimo()
    {
        foreach (var (nome, mapa) in Mapas.Pequenos(10))
        {
            var otimo = ForcaBruta.Melhor(mapa).Custo;

            foreach (var (heuristica, rodar) in Heuristicas.Todas())
                Assert.True(rodar(mapa).Custo >= otimo, $"{nome} com {heuristica}");
        }
    }

    /// <summary>
    /// O 2-OPT nunca PIORA a rota de onde ele parte: ele so troca quando a troca
    /// ganha.
    /// </summary>
    [Fact]
    public void ODoisOptNuncaPiora()
    {
        foreach (var (nome, mapa) in Mapas.Pequenos(10).Concat(Mapas.Grandes()))
        {
            var partida = Heuristicas.VizinhoMaisProximo(mapa);
            var depois = Heuristicas.DoisOpt(mapa, partida.Rota);

            Assert.True(depois.Custo <= partida.Custo, nome);
        }
    }

    /// <summary>
    /// O ACHADO PRINCIPAL: a rota do 2-OPT nao tem CRUZAMENTO, e a das outras duas
    /// tem.
    ///
    /// Duas arestas que se cruzam podem sempre ser destrocadas com ganho num mapa
    /// que obedece a desigualdade triangular, entao uma rota de onde nao da para
    /// melhorar nao pode ter cruzamento.
    ///
    /// E uma propriedade GEOMETRICA, conferida sobre os pontos do plano com um
    /// determinante EXATO em inteiros, e nao sobre a matriz de distancias.
    /// </summary>
    [Fact]
    public void ARotaDoDoisOptNaoTemCruzamento()
    {
        var comCruzamento = 0;

        foreach (var cidades in new[] { 20, 40, 60, 80 })
            for (ulong semente = 1; semente <= 8; semente++)
            {
                var (mapa, pontos) = Mapas.NoPlano(cidades, 1_000, semente);

                Assert.False(
                    Heuristicas.TemCruzamento(pontos, Heuristicas.VizinhoMais2Opt(mapa).Rota),
                    $"{cidades} cidades, semente {semente}");

                if (Heuristicas.TemCruzamento(pontos, Heuristicas.VizinhoMaisProximo(mapa).Rota)) comCruzamento++;
            }

        Assert.True(comCruzamento > 20, $"so {comCruzamento} rotas do vizinho tinham cruzamento");
    }

    /// <summary>
    /// O ACHADO QUE ME CORRIGIU: o CIRCULO, que eu montei para enganar o vizinho
    /// mais proximo, nao engana ninguem.
    ///
    /// A ideia era que ele andasse pela borda e atravessasse o diametro para fechar.
    /// Andar pela borda E a resposta otima, e a ultima aresta fecha o circulo em vez
    /// de atravessa-lo. As tres heuristicas acertam em cheio, em todo tamanho de dez
    /// a duzentas cidades.
    ///
    /// O mapa que separa de verdade e o plano sorteado, que e o caso realista e o
    /// menos vistoso.
    /// </summary>
    [Fact]
    public void OCirculoNaoEnganaNinguem()
    {
        foreach (var cidades in new[] { 10, 20, 50, 100, 200 })
        {
            var (mapa, otimo) = Mapas.NoCirculo(cidades, 10_000, 7);

            Assert.Equal(otimo, Heuristicas.VizinhoMaisProximo(mapa).Custo);
            Assert.Equal(otimo, Heuristicas.InsercaoMaisBarata(mapa).Custo);
            Assert.Equal(otimo, Heuristicas.VizinhoMais2Opt(mapa).Custo);
        }
    }

    /// <summary>
    /// E no PLANO SORTEADO os tres se separam: o vizinho erra 11% na media e ate
    /// 41% no pior mapa, e o 2-opt erra menos de 1% na media.
    /// </summary>
    [Fact]
    public void NoPlanoSorteadoOsTresSeSeparam()
    {
        var vizinho = new List<double>();
        var insercao = new List<double>();
        var comDoisOpt = new List<double>();

        for (ulong semente = 1; semente <= 60; semente++)
        {
            var (mapa, _) = Mapas.NoPlano(10, 1_000, semente);
            var otimo = (double)ForcaBruta.Melhor(mapa).Custo;

            vizinho.Add(Heuristicas.VizinhoMaisProximo(mapa).Custo / otimo);
            insercao.Add(Heuristicas.InsercaoMaisBarata(mapa).Custo / otimo);
            comDoisOpt.Add(Heuristicas.VizinhoMais2Opt(mapa).Custo / otimo);
        }

        Assert.True(vizinho.Average() > insercao.Average());
        Assert.True(insercao.Average() > comDoisOpt.Average());
        Assert.InRange(vizinho.Average(), 1.08, 1.20);
        Assert.InRange(comDoisOpt.Average(), 1.0, 1.02);
        Assert.True(vizinho.Max() > 1.3, $"o pior do vizinho foi {vizinho.Max():F2}");
    }

    /// <summary>
    /// A CIDADE DE PARTIDA muda o resultado do vizinho mais proximo, e e uma
    /// escolha que ninguem faz com cuidado.
    ///
    /// Entre a melhor e a pior partida ha ate trinta e tres por cento de diferenca,
    /// e o custo de testar todas e o custo de uma vezes o numero de cidades.
    /// </summary>
    [Fact]
    public void ACidadeDePartidaMudaOResultado()
    {
        var diferentes = 0;

        for (ulong semente = 1; semente <= 20; semente++)
        {
            var (mapa, _) = Mapas.NoPlano(10, 1_000, semente);
            var custos = new HashSet<long>();

            for (var c = 0; c < mapa.Cidades; c++) custos.Add(Heuristicas.VizinhoMaisProximo(mapa, c).Custo);

            if (custos.Count > 1) diferentes++;
        }

        Assert.True(diferentes >= 18, $"so {diferentes} de 20 mapas deram custos diferentes por partida");
    }

    [Fact]
    public void OsMapasDeUmaEDuasCidadesNaoQuebramNada()
    {
        foreach (var cidades in new[] { 1, 2 })
        {
            var mapa = new Mapa(cidades);

            if (cidades == 2) mapa.Ligar(0, 1, 5);

            foreach (var (nome, rodar) in Heuristicas.Todas())
            {
                var r = rodar(mapa);

                Assert.True(mapa.EhRotaValida(r.Rota), nome);
            }
        }
    }
}
