using Conde.Caixeiro;
using Xunit;

namespace Conde.Caixeiro.Testes;

/// <summary>
/// Os testes das TABELAS que o medidor imprime.
///
/// Eles existem porque numero em README que ninguem confere envelhece errado em
/// silencio: alguem troca uma constante, a tabela muda e o texto continua dizendo
/// a frase antiga. Aqui o teste afirma sobre o mesmo objeto que vira linha
/// impressa.
/// </summary>
public class MedidasTestes
{
    [Fact]
    public void OsDoisExatosBatemEmTodaLinha()
    {
        var linhas = Medidas.ContraADefinicao();

        Assert.Equal(14, linhas.Count);
        Assert.All(linhas, l => Assert.True(l.OsDoisExatosBatem, l.Mapa));
        Assert.All(linhas, l => Assert.True(l.Vizinho >= l.Otimo, l.Mapa));
        Assert.All(linhas, l => Assert.True(l.Insercao >= l.Otimo, l.Mapa));
        Assert.All(linhas, l => Assert.True(l.ComDoisOpt >= l.Otimo, l.Mapa));

        // os mapas geometricos obedecem a triangular e os sorteados nao
        Assert.All(linhas.Where(l => l.Mapa.StartsWith("sorteado")), l => Assert.False(l.Triangular));
        Assert.All(linhas.Where(l => l.Mapa.StartsWith("circulo")), l => Assert.True(l.Triangular));
    }

    /// <summary>
    /// A frase do texto: o circulo NAO engana ninguem, e as tres heuristicas
    /// acertam em cheio.
    /// </summary>
    [Fact]
    public void OCirculoNaoEnganaNinguem()
    {
        var linhas = Medidas.NoCirculo();

        Assert.Equal(5, linhas.Count);
        Assert.All(linhas, l => Assert.Equal(1.0, l.RazaoVizinho, 9));
        Assert.All(linhas, l => Assert.Equal(1.0, l.RazaoComDoisOpt, 9));
        Assert.All(linhas, l => Assert.Equal(l.Otimo, l.Insercao));
    }

    /// <summary>
    /// A frase do texto: no plano sorteado os tres se separam, e a ordem nao muda
    /// com o tamanho.
    /// </summary>
    [Fact]
    public void NoPlanoSorteadoOsTresSeSeparam()
    {
        var linhas = Medidas.Razoes(30);

        Assert.Equal(4, linhas.Count);

        foreach (var l in linhas)
        {
            Assert.True(l.MediaVizinho > l.MediaInsercao, $"{l.Cidades} cidades");
            Assert.True(l.MediaInsercao > l.MediaComDoisOpt, $"{l.Cidades} cidades");

            // nenhuma fica abaixo do otimo
            Assert.True(l.MediaComDoisOpt >= 1.0);
        }

        var dez = linhas.Single(l => l.Cidades == 10);

        Assert.InRange(dez.MediaVizinho, 1.08, 1.20);
        Assert.InRange(dez.MediaComDoisOpt, 1.0, 1.02);
        Assert.True(dez.PiorVizinho > 1.3, $"o pior do vizinho foi {dez.PiorVizinho:F2}");
    }

    /// <summary>
    /// A frase do texto: a rota do 2-opt NUNCA cruza, e a das outras duas cruza a
    /// partir de quarenta cidades.
    /// </summary>
    [Fact]
    public void ODoisOptNuncaCruzaEAsOutrasCruzam()
    {
        var linhas = Medidas.Cruzamentos();

        Assert.Equal(4, linhas.Count);
        Assert.All(linhas, l => Assert.False(l.ComDoisOptCruza, l.Mapa));

        // e as outras duas cruzam nos mapas maiores
        Assert.True(linhas.Count(l => l.VizinhoCruza) >= 2);
        Assert.True(linhas.Count(l => l.InsercaoCruza) >= 2);

        var oitenta = linhas.Single(l => l.Cidades == 80);

        Assert.True(oitenta.VizinhoCruza);
        Assert.True(oitenta.InsercaoCruza);
        Assert.False(oitenta.ComDoisOptCruza);
    }

    /// <summary>
    /// A frase do texto: a cidade de partida muda o resultado, e a melhor partida
    /// acerta o otimo nesses mapas.
    /// </summary>
    [Fact]
    public void ACidadeDePartidaMudaOResultado()
    {
        var linhas = Medidas.PorPartida();

        Assert.Equal(4, linhas.Count);
        Assert.All(linhas, l => Assert.True(l.Pior > l.Melhor, $"{l.Partidas} cidades"));
        Assert.All(linhas, l => Assert.True(l.RazaoDoPior > l.RazaoDoMelhor));

        // a pior partida chega a trinta por cento acima do otimo
        Assert.True(linhas.Max(l => l.RazaoDoPior) > 1.25);

        // e a melhor partida acerta o otimo em quase toda linha
        Assert.True(linhas.Count(l => l.RazaoDoMelhor < 1.01) >= 3);
    }

    /// <summary>
    /// A frase do texto: Held e Karp percorre 642 milhoes de vezes menos estados que
    /// a forca bruta com vinte cidades.
    /// </summary>
    [Fact]
    public void HeldKarpCresceMuitoMenosQueOFatorial()
    {
        var linhas = Medidas.Custos();

        Assert.Equal(7, linhas.Count);
        Assert.All(linhas, l => Assert.Equal(ForcaBruta.Fatorial(l.Cidades - 1), l.Rotas));

        var vinte = linhas.Single(l => l.Cidades == 20);

        Assert.Equal(121_645_100_408_832_000, vinte.Rotas);
        Assert.Equal(189_267_968, vinte.EstadosDeHeldKarp);
        Assert.True(vinte.QuantasVezesMenos > 600_000_000);

        // e com poucas cidades a forca bruta e MENOR: os estados so compensam depois
        var seis = linhas.Single(l => l.Cidades == 6);

        Assert.True(seis.Rotas < seis.EstadosDeHeldKarp);
    }

    /// <summary>
    /// A frase do texto: o 2-opt tira quase metade do custo num mapa sem geometria e
    /// nada num mapa com forma obvia.
    /// </summary>
    [Fact]
    public void OGanhoDoDoisOptDependeDoMapa()
    {
        var linhas = Medidas.Grandes();

        Assert.Equal(6, linhas.Count);
        Assert.All(linhas, l => Assert.True(l.ComDoisOpt <= l.Vizinho, l.Mapa));

        // no circulo e na grade ele nao tem o que melhorar
        Assert.Equal(0.0, linhas.Single(l => l.Mapa == "circulo de 200").GanhoDoDoisOpt, 9);
        Assert.Equal(0.0, linhas.Single(l => l.Mapa == "grade 20x20").GanhoDoDoisOpt, 9);

        // e no sorteado sem geometria ele tira quase metade
        Assert.True(linhas.Single(l => l.Mapa == "sorteado de 200").GanhoDoDoisOpt > 40);
    }
}
