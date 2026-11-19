using System.Collections.ObjectModel;

namespace DataPilot.Wpf.Helpers;

/// <summary>Atualiza uma coleção no lugar, por chave, para preservar seleção e rolagem (nada é recriado).</summary>
public static class SincronizacaoColecao
{
    public static void Sincronizar<TItem, TDado>(
        ObservableCollection<TItem> colecao,
        IReadOnlyList<TDado> dados,
        Func<TItem, string> chaveItem,
        Func<TDado, string> chaveDado,
        Func<TDado, TItem> criar,
        Action<TItem, TDado> atualizar)
    {
        var chaves = dados.Select(chaveDado).ToHashSet();
        for (var i = colecao.Count - 1; i >= 0; i--)
            if (!chaves.Contains(chaveItem(colecao[i]))) colecao.RemoveAt(i);

        for (var i = 0; i < dados.Count; i++)
        {
            var chave = chaveDado(dados[i]);
            var atual = -1;
            for (var j = i; j < colecao.Count; j++)
                if (chaveItem(colecao[j]) == chave) { atual = j; break; }

            if (atual < 0)
            {
                var novo = criar(dados[i]);
                atualizar(novo, dados[i]); // já preenchido quando entra na lista
                colecao.Insert(i, novo);
                continue;
            }
            if (atual != i) colecao.Move(atual, i);
            atualizar(colecao[i], dados[i]);
        }
    }
}
