namespace Conde.Caixeiro;

/// <summary>
/// O JUIZ, e ele e a definicao escrita do jeito mais burro possivel.
///
/// A melhor rota e a melhor, e saber que ela e a melhor exige olhar todas as
/// outras. Fixando a cidade de partida, sao (n-1) fatorial rotas: com dez cidades
/// sao 362.880, com treze sao quase meio bilhao.
///
/// Custa isso e precisa custar. O caixeiro viajante e o problema dificil mais
/// conhecido que existe, e um juiz rapido para ele seria uma noticia bem maior que
/// este repositorio.
///
/// E ha um SEGUNDO metodo exato aqui, o de Held e Karp, de 1962, que nao se parece
/// em nada com o primeiro: ele percorre SUBCONJUNTOS de cidades em vez de ordens,
/// e custa dois elevado a n vezes n ao quadrado. Com vinte cidades, um e um bilhao
/// de bilhoes de rotas e o outro sao quatrocentos milhoes de estados.
/// </summary>
public static class ForcaBruta
{
    /// <param name="Custo">o custo da melhor rota</param>
    /// <param name="Rota">uma rota desse custo</param>
    /// <param name="Olhadas">quantas rotas ou estados foram considerados</param>
    public record Resultado(long Custo, int[] Rota, long Olhadas);

    /// <summary>
    /// A MELHOR ROTA, por forca bruta sobre todas as ordens.
    ///
    /// A cidade zero fica fixa na partida. Nao e otimizacao opcional: sem isso cada
    /// rota aparece n vezes, uma por cidade de partida, e o custo medido seria o
    /// custo de nao ter fixado.
    /// </summary>
    public static Resultado Melhor(Mapa mapa)
    {
        if (mapa.Cidades > 11) throw new ArgumentException("forca bruta so ate 11 cidades", nameof(mapa));
        if (mapa.Cidades <= 1) return new Resultado(0, [.. Enumerable.Range(0, mapa.Cidades)], 0);

        var melhor = long.MaxValue;
        var melhorRota = Array.Empty<int>();
        var olhadas = 0L;

        foreach (var resto in Permutacoes(mapa.Cidades - 1))
        {
            olhadas++;

            var rota = new int[mapa.Cidades];

            rota[0] = 0;

            for (var i = 0; i < resto.Length; i++) rota[i + 1] = resto[i] + 1;

            var custo = mapa.Custo(rota);

            if (custo >= melhor) continue;

            melhor = custo;
            melhorRota = rota;
        }

        return new Resultado(melhor, melhorRota, olhadas);
    }

    /// <summary>
    /// O SEGUNDO METODO EXATO: HELD e KARP, de 1962.
    ///
    /// Em vez de percorrer ordens, ele percorre pares de (conjunto de cidades ja
    /// visitadas, cidade atual). O melhor caminho que sai de zero, visita exatamente
    /// aquele conjunto e para naquela cidade nao depende de COMO ele chegou la, e e
    /// so disso que a economia sai.
    ///
    /// Dois elevado a n vezes n ao quadrado continua sendo exponencial, e o problema
    /// continua dificil. Mas com vinte cidades a diferenca entre os dois metodos e a
    /// diferenca entre quatrocentos milhoes de estados e um bilhao de bilhoes de
    /// rotas.
    /// </summary>
    public static Resultado HeldKarp(Mapa mapa)
    {
        var n = mapa.Cidades;

        if (n > 20) throw new ArgumentException("Held e Karp so ate 20 cidades", nameof(mapa));
        if (n <= 1) return new Resultado(0, [.. Enumerable.Range(0, n)], 0);

        var estados = 1 << (n - 1);
        var melhor = new long[estados, n - 1];
        var veioDe = new int[estados, n - 1];
        var olhadas = 0L;

        for (var s = 0; s < estados; s++)
            for (var c = 0; c < n - 1; c++) melhor[s, c] = long.MaxValue;

        for (var c = 0; c < n - 1; c++) melhor[1 << c, c] = mapa[0, c + 1];

        for (var s = 1; s < estados; s++)
            for (var atual = 0; atual < n - 1; atual++)
            {
                if ((s & (1 << atual)) == 0 || melhor[s, atual] == long.MaxValue) continue;

                for (var proxima = 0; proxima < n - 1; proxima++)
                {
                    if ((s & (1 << proxima)) != 0) continue;

                    olhadas++;

                    var novo = s | (1 << proxima);
                    var custo = melhor[s, atual] + mapa[atual + 1, proxima + 1];

                    if (custo >= melhor[novo, proxima]) continue;

                    melhor[novo, proxima] = custo;
                    veioDe[novo, proxima] = atual;
                }
            }

        var completo = estados - 1;
        var fim = 0;
        var total = long.MaxValue;

        for (var c = 0; c < n - 1; c++)
        {
            if (melhor[completo, c] == long.MaxValue) continue;

            var custo = melhor[completo, c] + mapa[c + 1, 0];

            if (custo >= total) continue;

            total = custo;
            fim = c;
        }

        // a volta: desfazer o caminho guardado em veioDe
        var rota = new List<int>();
        var estado = completo;
        var cidade = fim;

        while (estado != 0)
        {
            rota.Add(cidade + 1);

            var anterior = veioDe[estado, cidade];
            var semEla = estado & ~(1 << cidade);

            estado = semEla;
            cidade = anterior;
        }

        rota.Add(0);
        rota.Reverse();

        return new Resultado(total, [.. rota], olhadas);
    }

    /// <summary>
    /// Todas as ordens de n elementos, pelo algoritmo de Heap, SEM RECURSAO.
    ///
    /// Sem recursao porque a profundidade seria o numero de cidades e porque o
    /// algoritmo de Heap iterativo gera cada permutacao com UMA troca a partir da
    /// anterior, o que e o que torna possivel percorrer milhoes delas.
    /// </summary>
    public static IEnumerable<int[]> Permutacoes(int n)
    {
        var atual = Enumerable.Range(0, n).ToArray();
        var contador = new int[n];

        yield return atual;

        var i = 0;

        while (i < n)
        {
            if (contador[i] >= i) { contador[i] = 0; i++; continue; }

            var j = i % 2 == 0 ? 0 : contador[i];

            (atual[i], atual[j]) = (atual[j], atual[i]);

            yield return atual;

            contador[i]++;
            i = 0;
        }
    }

    public static long Fatorial(int n)
    {
        var f = 1L;

        for (var i = 2; i <= n; i++) f *= i;

        return f;
    }
}
