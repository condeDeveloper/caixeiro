using Conde.Caixeiro;
using Xunit;

namespace Conde.Caixeiro.Testes;

/// <summary>
/// O juiz tambem precisa de juiz.
///
/// Todo veredicto deste repositorio e "a melhor rota custa tanto". Se a forca bruta
/// tivesse um erro, as heuristicas seriam julgadas contra uma referencia errada, e
/// o erro mais provavel dela e devolver um custo PEQUENO demais, por uma rota que
/// nao passa por todas as cidades.
///
/// Por isso os testes daqui conferem tres coisas separadas: que a rota devolvida e
/// valida e custa o anunciado; que nenhuma rota melhor existe; e que um SEGUNDO
/// metodo exato, que percorre um espaco completamente diferente, da o mesmo custo.
/// </summary>
public class ForcaBrutaTestes
{
    [Fact]
    public void ARotaDevolvidaEhValidaECustaOAnunciado()
    {
        foreach (var (nome, mapa) in Mapas.Pequenos(10))
        {
            var r = ForcaBruta.Melhor(mapa);

            Assert.True(mapa.EhRotaValida(r.Rota), nome);
            Assert.Equal(r.Custo, mapa.Custo(r.Rota));
        }
    }

    /// <summary>
    /// E nenhuma rota MELHOR existe: o teste tenta TODAS, sem fixar a cidade de
    /// partida.
    ///
    /// Fixar a partida e uma otimizacao que nao muda a resposta, e este teste existe
    /// justamente para provar isso: toda rota, com qualquer cidade na frente, custa
    /// pelo menos o que a busca devolveu.
    /// </summary>
    [Fact]
    public void NenhumaRotaMelhorExisteNemComOutraPartida()
    {
        foreach (var (nome, mapa) in Mapas.Pequenos(3))
        {
            if (mapa.Cidades > 8) continue;

            var melhor = ForcaBruta.Melhor(mapa).Custo;

            foreach (var rota in ForcaBruta.Permutacoes(mapa.Cidades))
                Assert.True(mapa.Custo(rota) >= melhor, $"{nome}: achei {mapa.Custo(rota)}");
        }
    }

    /// <summary>
    /// O SEGUNDO METODO EXATO, de HELD e KARP, da o mesmo custo, e ele percorre um
    /// espaco completamente diferente.
    ///
    /// Um percorre as ORDENS de visita; o outro percorre pares de (conjunto de
    /// cidades ja visitadas, cidade atual) e nunca escreve uma rota ate o fim.
    /// Quando duas respostas independentes batem em todo mapa, o que esta certo nao
    /// e a implementacao.
    /// </summary>
    [Fact]
    public void OsDoisMetodosExatosDaoOMesmoCusto()
    {
        foreach (var (nome, mapa) in Mapas.Pequenos(15))
            Assert.Equal(ForcaBruta.Melhor(mapa).Custo, ForcaBruta.HeldKarp(mapa).Custo);

        for (ulong semente = 1; semente <= 40; semente++)
        {
            var (mapa, _) = Mapas.NoPlano(9, 500, semente);

            Assert.Equal(ForcaBruta.Melhor(mapa).Custo, ForcaBruta.HeldKarp(mapa).Custo);
        }
    }

    /// <summary>
    /// E a ROTA que Held e Karp devolve e valida e custa o que ele anuncia.
    ///
    /// Ele calcula o custo por programacao dinamica e so depois desfaz a tabela para
    /// dizer a rota. Se essa volta tivesse um erro, o custo continuaria certo e a
    /// rota estaria errada, e nada avisaria.
    /// </summary>
    [Fact]
    public void ARotaDeHeldKarpEhValidaECustaOAnunciado()
    {
        foreach (var (nome, mapa) in Mapas.Pequenos(15))
        {
            var r = ForcaBruta.HeldKarp(mapa);

            Assert.True(mapa.EhRotaValida(r.Rota), nome);
            Assert.Equal(r.Custo, mapa.Custo(r.Rota));
        }

        for (ulong semente = 1; semente <= 20; semente++)
        {
            var (mapa, _) = Mapas.NoPlano(14, 500, semente);
            var r = ForcaBruta.HeldKarp(mapa);

            Assert.True(mapa.EhRotaValida(r.Rota));
            Assert.Equal(r.Custo, mapa.Custo(r.Rota));
        }
    }

    /// <summary>
    /// HELD e KARP chega onde a forca bruta nao chega: catorze, dezesseis, dezoito
    /// cidades.
    ///
    /// Com dezoito cidades sao 355 trilhoes de rotas e 38 milhoes de estados. Os
    /// dois sao exponenciais, e os dois resolvem o mesmo problema: a diferenca e a
    /// base da exponencial.
    /// </summary>
    [Fact]
    public void HeldKarpChegaOndeAForcaBrutaNaoChega()
    {
        foreach (var cidades in new[] { 14, 16, 18 })
        {
            var (mapa, _) = Mapas.NoPlano(cidades, 1_000, 7);
            var r = ForcaBruta.HeldKarp(mapa);

            Assert.True(mapa.EhRotaValida(r.Rota), $"{cidades} cidades");
            Assert.Equal(r.Custo, mapa.Custo(r.Rota));

            // e a heuristica nao fica abaixo dele
            Assert.True(Heuristicas.VizinhoMais2Opt(mapa).Custo >= r.Custo);
        }

        Assert.Throws<ArgumentException>(() => ForcaBruta.Melhor(new Mapa(14)));
    }

    [Fact]
    public void AsPermutacoesSaoTodasEDiferentes()
    {
        for (var n = 1; n <= 7; n++)
        {
            var todas = ForcaBruta.Permutacoes(n).Select(p => string.Join(",", p)).ToList();

            Assert.Equal(ForcaBruta.Fatorial(n), todas.Count);
            Assert.Equal(ForcaBruta.Fatorial(n), todas.Distinct().Count());
        }
    }

    [Fact]
    public void OsExatosRecusamMapaGrandeDemaisEmVezDeDemorarPraSempre()
    {
        Assert.Throws<ArgumentException>(() => ForcaBruta.Melhor(new Mapa(15)));
        Assert.Throws<ArgumentException>(() => ForcaBruta.HeldKarp(new Mapa(25)));
    }

    [Fact]
    public void OsMapasDeUmaCidadeNaoQuebramNada()
    {
        Assert.Equal(0, ForcaBruta.Melhor(new Mapa(1)).Custo);
        Assert.Equal(0, ForcaBruta.HeldKarp(new Mapa(1)).Custo);
    }
}
