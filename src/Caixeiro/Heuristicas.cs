namespace Conde.Caixeiro;

/// <summary>
/// AS HEURISTICAS, e o que separa uma da outra e quando elas decidem.
///
/// - O VIZINHO MAIS PROXIMO decide tudo na hora: va sempre para a cidade mais
///   perto que ainda nao foi visitada. Ele e a primeira ideia de todo mundo, e a
///   ultima aresta dele e sempre a mais cara que sobrou.
///
/// - A INSERCAO MAIS BARATA monta a rota de fora para dentro: comece com duas
///   cidades e, a cada passo, encaixe a proxima no lugar que aumenta menos o
///   custo. Ela decide onde, e nao quando.
///
/// - O 2-OPT nao monta rota nenhuma: ele pega uma rota pronta e desfaz todo
///   cruzamento, invertendo trechos, ate nao haver mais o que melhorar.
///
/// O ultimo e o unico que tem uma propriedade util e facil de enunciar: a rota que
/// ele devolve NAO SE CRUZA, num mapa que obedece a desigualdade triangular.
/// </summary>
public static class Heuristicas
{
    /// <param name="Rota">a ordem de visita</param>
    /// <param name="Custo">o custo dela</param>
    /// <param name="Olhadas">quantas distancias foram consultadas</param>
    public record Resultado(int[] Rota, long Custo, long Olhadas);

    /// <summary>
    /// O VIZINHO MAIS PROXIMO: va sempre para a cidade mais perto que sobrou.
    ///
    /// Ele tem uma garantia, e ela e ruim: num mapa que obedece a desigualdade
    /// triangular, ele nao passa de metade do logaritmo do numero de cidades vezes
    /// o otimo. Isso CRESCE com o tamanho, e a tabela do medidor mostra a rota dele
    /// no circulo, onde o erro e visivel a olho nu.
    /// </summary>
    public static Resultado VizinhoMaisProximo(Mapa mapa, int comeco = 0)
    {
        var visitada = new bool[mapa.Cidades];
        var rota = new List<int> { comeco };
        var olhadas = 0L;

        visitada[comeco] = true;

        for (var passo = 1; passo < mapa.Cidades; passo++)
        {
            var atual = rota[^1];
            var melhor = -1;

            for (var c = 0; c < mapa.Cidades; c++)
            {
                if (visitada[c]) continue;

                olhadas++;

                if (melhor < 0 || mapa[atual, c] < mapa[atual, melhor]) melhor = c;
            }

            visitada[melhor] = true;
            rota.Add(melhor);
        }

        return new Resultado([.. rota], mapa.Custo(rota), olhadas);
    }

    /// <summary>
    /// A INSERCAO MAIS BARATA: encaixe cada cidade nova onde ela aumenta menos o
    /// custo.
    ///
    /// Ela tem garantia melhor: num mapa que obedece a desigualdade triangular, ela
    /// nao passa de DUAS vezes o otimo, e esse limite nao cresce com o tamanho.
    ///
    /// A diferenca entre as duas e quando a decisao e tomada. O vizinho mais proximo
    /// decide a posicao de uma cidade no momento em que a visita, e nunca mais
    /// mexe; a insercao decide no momento em que ela entra, com a rota inteira ja
    /// montada para olhar.
    /// </summary>
    public static Resultado InsercaoMaisBarata(Mapa mapa)
    {
        if (mapa.Cidades <= 2) return Trivial(mapa);

        var rota = new List<int> { 0, 1 };
        var dentro = new bool[mapa.Cidades];
        var olhadas = 0L;

        dentro[0] = dentro[1] = true;

        while (rota.Count < mapa.Cidades)
        {
            var melhorCidade = -1;
            var melhorOnde = 0;
            var melhorAumento = long.MaxValue;

            for (var c = 0; c < mapa.Cidades; c++)
            {
                if (dentro[c]) continue;

                for (var i = 0; i < rota.Count; i++)
                {
                    var a = rota[i];
                    var b = rota[(i + 1) % rota.Count];

                    olhadas++;

                    var aumento = mapa[a, c] + mapa[c, b] - mapa[a, b];

                    if (aumento >= melhorAumento) continue;

                    melhorAumento = aumento;
                    melhorCidade = c;
                    melhorOnde = i + 1;
                }
            }

            rota.Insert(melhorOnde, melhorCidade);
            dentro[melhorCidade] = true;
        }

        return new Resultado([.. rota], mapa.Custo(rota), olhadas);
    }

    /// <summary>
    /// O 2-OPT: inverta trechos enquanto isso melhorar.
    ///
    /// Ele nao monta rota: ele conserta uma. Pegue dois pares de cidades vizinhas na
    /// rota, troque as ligacoes e inverta o trecho do meio; se o custo caiu, fique
    /// com a troca.
    ///
    /// O que ele garante e simples de enunciar e nada obvio: num mapa que obedece a
    /// desigualdade triangular, a rota que ele devolve nao tem CRUZAMENTO nenhum.
    /// Duas arestas que se cruzam podem sempre ser destrocadas com ganho, entao uma
    /// rota de onde nao da para melhorar nao pode ter cruzamento.
    /// </summary>
    public static Resultado DoisOpt(Mapa mapa, IReadOnlyList<int> inicial, int limite = 100_000)
    {
        var rota = inicial.ToArray();
        var olhadas = 0L;
        var melhorou = true;
        var rodadas = 0;

        while (melhorou && rodadas < limite)
        {
            melhorou = false;
            rodadas++;

            for (var i = 0; i < rota.Length - 1 && !melhorou; i++)
                for (var j = i + 2; j < rota.Length; j++)
                {
                    if (i == 0 && j == rota.Length - 1) continue;

                    olhadas++;

                    var a = rota[i];
                    var b = rota[i + 1];
                    var c = rota[j];
                    var d = rota[(j + 1) % rota.Length];

                    var antes = mapa[a, b] + mapa[c, d];
                    var depois = mapa[a, c] + mapa[b, d];

                    if (depois >= antes) continue;

                    Array.Reverse(rota, i + 1, j - i);
                    melhorou = true;

                    break;
                }
        }

        return new Resultado(rota, mapa.Custo(rota), olhadas);
    }

    /// <summary>O 2-opt partindo do vizinho mais proximo, que e como ele e usado de verdade.</summary>
    public static Resultado VizinhoMais2Opt(Mapa mapa)
    {
        var vizinho = VizinhoMaisProximo(mapa);
        var melhorado = DoisOpt(mapa, vizinho.Rota);

        return new Resultado(melhorado.Rota, melhorado.Custo, vizinho.Olhadas + melhorado.Olhadas);
    }

    /// <summary>A rota na ordem dos indices: o chao da tabela.</summary>
    public static Resultado NaOrdem(Mapa mapa)
    {
        var rota = Enumerable.Range(0, mapa.Cidades).ToArray();

        return new Resultado(rota, mapa.Custo(rota), 0);
    }

    private static Resultado Trivial(Mapa mapa)
    {
        var rota = Enumerable.Range(0, mapa.Cidades).ToArray();

        return new Resultado(rota, mapa.Custo(rota), 0);
    }

    public static IEnumerable<(string Nome, Func<Mapa, Resultado> Rodar)> Todas()
    {
        yield return ("na ordem", NaOrdem);
        yield return ("vizinho mais proximo", m => VizinhoMaisProximo(m));
        yield return ("insercao mais barata", InsercaoMaisBarata);
        yield return ("vizinho mais 2-opt", VizinhoMais2Opt);
    }

    /// <summary>
    /// A rota TEM CRUZAMENTO? Duas arestas que se cruzam no plano.
    ///
    /// E a conferencia da propriedade do 2-opt, e ela so faz sentido com os pontos
    /// na mao: cruzamento e uma coisa do desenho, nao da matriz de distancias.
    /// </summary>
    public static bool TemCruzamento((int X, int Y)[] pontos, IReadOnlyList<int> rota)
    {
        for (var i = 0; i < rota.Count; i++)
            for (var j = i + 1; j < rota.Count; j++)
            {
                var a = pontos[rota[i]];
                var b = pontos[rota[(i + 1) % rota.Count]];
                var c = pontos[rota[j]];
                var d = pontos[rota[(j + 1) % rota.Count]];

                // segmentos que compartilham ponta nao contam como cruzamento
                if (rota[i] == rota[j] || rota[i] == rota[(j + 1) % rota.Count]) continue;
                if (rota[(i + 1) % rota.Count] == rota[j]) continue;
                if (rota[(i + 1) % rota.Count] == rota[(j + 1) % rota.Count]) continue;

                if (SeCruzam(a, b, c, d)) return true;
            }

        return false;
    }

    private static bool SeCruzam((int X, int Y) a, (int X, int Y) b, (int X, int Y) c, (int X, int Y) d)
    {
        var d1 = Lado(c, d, a);
        var d2 = Lado(c, d, b);
        var d3 = Lado(a, b, c);
        var d4 = Lado(a, b, d);

        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    /// <summary>De que lado da reta o ponto esta, pelo determinante EXATO em inteiros.</summary>
    private static long Lado((int X, int Y) a, (int X, int Y) b, (int X, int Y) p) =>
        (long)(b.X - a.X) * (p.Y - a.Y) - (long)(b.Y - a.Y) * (p.X - a.X);
}
