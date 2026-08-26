namespace PxOperations.Domain.Nps.Calculation;

public static class NpsCollectionStatusCalculator
{
    /// <summary>
    /// Ordem de precedência: quem já respondeu é "respondido", ainda que o
    /// prazo tenha estourado; sem resposta, um link aberto é "link gerado"; e
    /// sem nenhum dos dois a coleta está pendente.
    /// </summary>
    public static NpsCollectionStatus Derive(bool hasResponse, int activeDispatches)
    {
        if (hasResponse)
        {
            return NpsCollectionStatus.Answered;
        }

        return activeDispatches > 0 ? NpsCollectionStatus.LinkSent : NpsCollectionStatus.Pending;
    }
}
