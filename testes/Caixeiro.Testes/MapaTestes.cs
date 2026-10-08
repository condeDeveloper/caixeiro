using Conde.Caixeiro;
using Xunit;

namespace Conde.Caixeiro.Testes;

public class MapaTestes
{
    [Fact]
    public void OSorteioEhDeterminista()
    {
        var a = new Sorteio(7);
        var b = new Sorteio(7);

        for (var i = 0; i < 1000; i++) Assert.Equal(a.Proximo(), b.Proximo());
    }

    /// <summary>
    /// A fracao precisa cobrir os DOIS LADOS do meio: um deslocamento de 33 em vez
    /// de 32 deixaria o gerador com 31 bits, e os pontos sorteados ficariam todos
    /// no quadrante de baixo, com as distancias continuando plausiveis.
    /// </summary>
    [Fact]
    public void AFracaoCobreOsDoisLadosDoMeio()
    {
        var sorteio = new Sorteio(7);
        var acima = 0;
        var maior = 0.0;

        for (var i = 0; i < 10_000; i++)
        {
            var f = sorteio.Fracao();

            Assert.InRange(f, 0.0, 1.0);
            if (f > 0.5) acima++;
            if (f > maior) maior = f;
        }

        Assert.InRange(acima, 4_700, 5_300);
        Assert.True(maior > 0.999, $"o maior sorteio foi {maior}");
    }

    /// <summary>
    /// O CUSTO de uma rota inclui a VOLTA para o comeco, e isso nao e detalhe.
    ///
    /// Sem ela o problema vira outro, o do caminho mais curto que passa por todos, e
    /// os dois tem respostas diferentes: num mapa em linha, o caminho custa o
    /// comprimento e o ciclo custa o dobro.
    /// </summary>
    [Fact]
    public void OCustoIncluiAVoltaParaOComeco()
    {
        var mapa = new Mapa(3);

        mapa.Ligar(0, 1, 10);
        mapa.Ligar(1, 2, 20);
        mapa.Ligar(0, 2, 30);

        Assert.Equal(60, mapa.Custo([0, 1, 2]));
        Assert.Equal(60, mapa.Custo([1, 2, 0]));
        Assert.Equal(60, mapa.Custo([2, 1, 0]));
    }

    [Fact]
    public void UmaRotaQueNaoPassaPorTodasEhRecusada()
    {
        var mapa = new Mapa(3);

        Assert.Throws<ArgumentException>(() => mapa.Custo([0, 1]));
        Assert.False(mapa.EhRotaValida([0, 1]));
        Assert.False(mapa.EhRotaValida([0, 1, 1]));
        Assert.True(mapa.EhRotaValida([2, 0, 1]));
    }

    [Fact]
    public void ADistanciaDeUmaCidadeAElaMesmaNaoSeLiga() =>
        Assert.Throws<ArgumentException>(() => new Mapa(3).Ligar(1, 1, 5));

    /// <summary>
    /// Os mapas GEOMETRICOS obedecem a desigualdade triangular e os SORTEADOS nao.
    ///
    /// Isso importa porque as garantias das heuristicas dependem dela: afirmar que
    /// a insercao fica dentro de duas vezes o otimo num mapa sorteado seria
    /// publicar uma garantia que o mapa nao sustenta.
    /// </summary>
    [Fact]
    public void OsMapasGeometricosObedecemATriangularEOsSorteadosNao()
    {
        for (ulong semente = 1; semente <= 10; semente++)
        {
            Assert.True(Mapas.NoPlano(10, 100, semente).Mapa.ObedeceATriangular(), "plano");
            Assert.True(Mapas.NoCirculo(10, 1_000, semente).Mapa.ObedeceATriangular(), "circulo");
        }

        Assert.True(Mapas.NaGrade(3, 4).Mapa.ObedeceATriangular());

        var quebrou = 0;

        for (ulong semente = 1; semente <= 20; semente++)
            if (!Mapas.Sorteado(9, 100, semente).ObedeceATriangular()) quebrou++;

        Assert.True(quebrou > 15, $"so {quebrou} de 20 mapas sorteados quebraram a triangular");
    }

    /// <summary>
    /// O CIRCULO tem a numeracao EMBARALHADA, e sem isso qualquer algoritmo
    /// acertaria por acaso seguindo a ordem dos indices.
    /// </summary>
    [Fact]
    public void ONumeroDasCidadesDoCirculoEstaEmbaralhado()
    {
        var (mapa, otimo) = Mapas.NoCirculo(20, 1_000, 7);
        var naOrdem = Heuristicas.NaOrdem(mapa).Custo;

        Assert.True(naOrdem > 3 * otimo, $"na ordem {naOrdem}, otimo {otimo}");
    }

    /// <summary>
    /// E o otimo que o gerador do circulo anuncia e mesmo o otimo, conferido contra
    /// a forca bruta nos tamanhos em que ela cabe.
    ///
    /// Sem isso, o "otimo" da tabela seria uma afirmacao sobre a construcao e nao
    /// sobre o mapa.
    /// </summary>
    [Fact]
    public void OOtimoAnunciadoDoCirculoEhMesmoOOtimo()
    {
        for (var cidades = 4; cidades <= 10; cidades++)
            for (ulong semente = 1; semente <= 5; semente++)
            {
                var (mapa, otimo) = Mapas.NoCirculo(cidades, 1_000, semente);

                Assert.Equal(otimo, ForcaBruta.Melhor(mapa).Custo);
            }
    }

    /// <summary>
    /// E o da GRADE tambem, quando um dos lados e par.
    ///
    /// A grade de tres por quatro tem doze cidades, e a forca bruta so vai ate onze:
    /// quem julga essa linha e Held e Karp, que e o outro metodo exato. E ter dois
    /// juizes com alcances diferentes e justamente o que permite conferir onde um
    /// deles nao chega.
    /// </summary>
    [Fact]
    public void OOtimoAnunciadoDaGradeEhMesmoOOtimo()
    {
        foreach (var (linhas, colunas) in new[] { (2, 3), (2, 4), (2, 5), (3, 4), (2, 8), (4, 4) })
        {
            var (mapa, otimo) = Mapas.NaGrade(linhas, colunas);

            Assert.Equal(linhas * colunas, otimo);
            Assert.Equal(otimo, ForcaBruta.HeldKarp(mapa).Custo);

            if (mapa.Cidades <= 11) Assert.Equal(otimo, ForcaBruta.Melhor(mapa).Custo);
        }
    }
}
