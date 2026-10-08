namespace Conde.Caixeiro;

/// <summary>
/// O SORTEIO, escrito aqui em vez de vir da biblioteca.
///
/// A documentacao do sorteio de biblioteca nao promete a mesma sequencia entre
/// versoes nem entre sistemas, e a integracao continua roda em tres. "O vizinho
/// mais proximo ficou 24% acima do otimo" com mapas diferentes em cada sistema nao
/// e uma medida: sao tres medidas que ninguem pode comparar.
/// </summary>
public sealed class Sorteio
{
    private ulong estado;

    public Sorteio(ulong semente) => estado = semente == 0 ? 1 : semente;

    public uint Proximo()
    {
        estado = estado * 6364136223846793005UL + 1442695040888963407UL;
        return (uint)(estado >> 32);
    }

    public int Ate(int limite) => (int)(Proximo() % (uint)limite);

    public double Fracao() => Proximo() / 4294967296.0;
}

/// <summary>
/// Um MAPA: as distancias entre cada par de cidades, em numeros INTEIROS.
///
/// Inteiras de proposito. O problema do caixeiro e uma comparacao de somas, e com
/// ponto flutuante duas rotas de mesmo custo podem sair com custos diferentes
/// dependendo da ORDEM em que as parcelas foram somadas. Um algoritmo que escolhe
/// a menor passaria a depender da ordem de visita, e a tabela deixaria de dizer
/// qualquer coisa sobre o algoritmo.
///
/// Com inteiros, duas rotas com as mesmas arestas tem exatamente o mesmo custo, e
/// "o otimo" e um numero e nao uma faixa.
/// </summary>
public sealed class Mapa
{
    private readonly long[,] distancia;

    public Mapa(int cidades)
    {
        Cidades = cidades;
        distancia = new long[cidades, cidades];
    }

    public int Cidades { get; }

    public long this[int de, int para] => distancia[de, para];

    public void Ligar(int a, int b, long quanto)
    {
        if (a == b) throw new ArgumentException("a distancia de uma cidade a ela mesma e zero", nameof(a));
        if (quanto < 0) throw new ArgumentOutOfRangeException(nameof(quanto));

        distancia[a, b] = quanto;
        distancia[b, a] = quanto;
    }

    /// <summary>
    /// O CUSTO de uma rota: a soma das distancias, VOLTANDO para o comeco.
    ///
    /// A volta nao e detalhe. Sem ela o problema vira outro, o do caminho mais
    /// curto que passa por todos, e os dois tem respostas diferentes: num mapa em
    /// linha, o caminho custa o comprimento e o ciclo custa o dobro.
    /// </summary>
    public long Custo(IReadOnlyList<int> rota)
    {
        if (rota.Count != Cidades) throw new ArgumentException("a rota nao passa por todas as cidades", nameof(rota));

        var total = 0L;

        for (var i = 0; i < rota.Count; i++) total += distancia[rota[i], rota[(i + 1) % rota.Count]];

        return total;
    }

    /// <summary>Uma rota e valida quando ela passa por cada cidade exatamente uma vez.</summary>
    public bool EhRotaValida(IReadOnlyList<int> rota) =>
        rota.Count == Cidades && rota.Distinct().Count() == Cidades && rota.All(c => c >= 0 && c < Cidades);

    /// <summary>
    /// O mapa obedece a DESIGUALDADE TRIANGULAR: ir direto nunca e pior que
    /// desviar.
    ///
    /// Ela parece obvia e nao vale sempre: um mapa de precos de passagem aerea
    /// quebra ela o tempo todo. E ela e a condicao de varias garantias, entao
    /// afirmar "esta heuristica fica dentro de duas vezes o otimo" sem conferir isso
    /// seria publicar uma garantia que o mapa nao sustenta.
    /// </summary>
    public bool ObedeceATriangular()
    {
        for (var a = 0; a < Cidades; a++)
            for (var b = 0; b < Cidades; b++)
                for (var c = 0; c < Cidades; c++)
                    if (distancia[a, c] > distancia[a, b] + distancia[b, c]) return false;

        return true;
    }
}

/// <summary>
/// Os MAPAS que separam as heuristicas, e por que eles sao assim.
///
/// Mapa de distancias sorteadas sem geometria nenhuma nao separa quase nada, e
/// nem obedece a desigualdade triangular. O que separa sao os mapas com forma: o
/// CIRCULO, em que a resposta certa e obvia para um humano e nada obvia para uma
/// heuristica, e a GRADE, em que ela e obvia e tem empate em todo lugar.
/// </summary>
public static class Mapas
{
    /// <summary>Distancias sorteadas, sem geometria: nao obedece a triangular.</summary>
    public static Mapa Sorteado(int cidades, long maior, ulong semente)
    {
        var sorteio = new Sorteio(semente);
        var mapa = new Mapa(cidades);

        for (var a = 0; a < cidades; a++)
            for (var b = a + 1; b < cidades; b++) mapa.Ligar(a, b, 1 + sorteio.Ate((int)maior));

        return mapa;
    }

    /// <summary>
    /// Pontos sorteados num quadrado, com a distancia EUCLIDIANA arredondada.
    ///
    /// E o caso realista, e ele obedece a desigualdade triangular por construcao
    /// geometrica. O arredondamento e para cima de proposito: arredondar para o
    /// mais perto pode quebrar a triangular por um, e uma garantia que depende dela
    /// deixaria de valer por causa do arredondamento.
    /// </summary>
    public static (Mapa Mapa, (int X, int Y)[] Pontos) NoPlano(int cidades, int lado, ulong semente)
    {
        var sorteio = new Sorteio(semente);
        var pontos = new (int X, int Y)[cidades];

        for (var i = 0; i < cidades; i++) pontos[i] = (sorteio.Ate(lado), sorteio.Ate(lado));

        var mapa = new Mapa(cidades);

        for (var a = 0; a < cidades; a++)
            for (var b = a + 1; b < cidades; b++)
            {
                var dx = (long)pontos[a].X - pontos[b].X;
                var dy = (long)pontos[a].Y - pontos[b].Y;

                mapa.Ligar(a, b, (long)Math.Ceiling(Math.Sqrt(dx * dx + dy * dy)));
            }

        return (mapa, pontos);
    }

    /// <summary>
    /// Cidades num CIRCULO, embaralhadas na numeracao.
    ///
    /// A rota otima e dar a volta, e um humano a ve num piscar. Uma heuristica que
    /// olha so a proxima cidade mais perto nao a ve: ela anda pelo circulo e, quando
    /// precisa fechar, atravessa o diametro.
    ///
    /// A numeracao embaralhada e o que impede qualquer algoritmo de acertar por
    /// acaso seguindo a ordem dos indices.
    /// </summary>
    public static (Mapa Mapa, long Otimo) NoCirculo(int cidades, int raio, ulong semente)
    {
        var sorteio = new Sorteio(semente);
        var ordem = Enumerable.Range(0, cidades).ToArray();

        for (var i = cidades - 1; i > 0; i--)
        {
            var j = sorteio.Ate(i + 1);

            (ordem[i], ordem[j]) = (ordem[j], ordem[i]);
        }

        var pontos = new (double X, double Y)[cidades];

        for (var i = 0; i < cidades; i++)
        {
            var angulo = 2 * Math.PI * i / cidades;

            pontos[ordem[i]] = (raio * Math.Cos(angulo), raio * Math.Sin(angulo));
        }

        var mapa = new Mapa(cidades);

        for (var a = 0; a < cidades; a++)
            for (var b = a + 1; b < cidades; b++)
            {
                var dx = pontos[a].X - pontos[b].X;
                var dy = pontos[a].Y - pontos[b].Y;

                mapa.Ligar(a, b, (long)Math.Round(Math.Sqrt(dx * dx + dy * dy)));
            }

        var otimo = 0L;

        for (var i = 0; i < cidades; i++) otimo += mapa[ordem[i], ordem[(i + 1) % cidades]];

        return (mapa, otimo);
    }

    /// <summary>
    /// Cidades numa GRADE, com distancia de quarteirao.
    ///
    /// E o mapa com mais EMPATES que existe: muitas rotas diferentes custam
    /// exatamente a mesma coisa, e isso separa um algoritmo que devolve "a melhor"
    /// de um que devolve "uma das melhores".
    /// </summary>
    public static (Mapa Mapa, long Otimo) NaGrade(int linhas, int colunas)
    {
        var cidades = linhas * colunas;
        var mapa = new Mapa(cidades);

        for (var a = 0; a < cidades; a++)
            for (var b = a + 1; b < cidades; b++)
            {
                var (la, ca) = (a / colunas, a % colunas);
                var (lb, cb) = (b / colunas, b % colunas);

                mapa.Ligar(a, b, Math.Abs(la - lb) + Math.Abs(ca - cb));
            }

        // numa grade com um lado par, dar a volta em serpentina custa o numero de
        // cidades: cada passo anda um quarteirao e a volta fecha de graca
        var otimo = linhas % 2 == 0 || colunas % 2 == 0 ? cidades : -1;

        return (mapa, otimo);
    }

    public static IEnumerable<(string Nome, Mapa Mapa)> Pequenos(int sorteados = 15)
    {
        yield return ("circulo de 8", NoCirculo(8, 1_000, 7).Mapa);
        yield return ("circulo de 10", NoCirculo(10, 1_000, 11).Mapa);
        yield return ("grade 2x4", NaGrade(2, 4).Mapa);
        yield return ("grade 3x3", NaGrade(3, 3).Mapa);
        yield return ("plano de 9", NoPlano(9, 100, 3).Mapa);
        yield return ("plano de 10", NoPlano(10, 100, 5).Mapa);

        for (ulong semente = 1; semente <= (ulong)sorteados; semente++)
            yield return ($"sorteado de 9 ({semente})", Sorteado(9, 100, semente));
    }

    public static IEnumerable<(string Nome, Mapa Mapa)> Grandes()
    {
        yield return ("circulo de 200", NoCirculo(200, 10_000, 7).Mapa);
        yield return ("grade 10x10", NaGrade(10, 10).Mapa);
        yield return ("grade 20x20", NaGrade(20, 20).Mapa);
        yield return ("plano de 100", NoPlano(100, 1_000, 11).Mapa);
        yield return ("plano de 500", NoPlano(500, 10_000, 13).Mapa);
        yield return ("sorteado de 200", Sorteado(200, 1_000, 17));
    }
}
