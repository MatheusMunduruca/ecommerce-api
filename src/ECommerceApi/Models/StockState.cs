namespace ECommerceApi.Models;

/// <summary>
/// Guarda qual janela de 6 horas já teve o estoque randomizado.
/// Permite re-sortear o estoque mesmo que a API tenha ficado offline:
/// ao voltar, compara a janela atual com a última registrada.
/// </summary>
public class StockState
{
    public int Id { get; set; }
    public long LastWindow { get; set; }
    public DateTime LastRandomizedAt { get; set; }
}
