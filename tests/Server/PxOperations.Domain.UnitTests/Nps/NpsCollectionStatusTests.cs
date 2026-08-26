using PxOperations.Domain.Nps;
using PxOperations.Domain.Nps.Calculation;

namespace PxOperations.Domain.UnitTests.Nps;

/// <summary>
/// F1 oferece o status da coleta como faceta. A derivação vive no domínio
/// porque cliente e servidor precisam da MESMA definição: enquanto a tela
/// derivava por conta própria, filtrar no servidor significaria escrever a
/// regra duas vezes, e duas cópias divergem.
/// </summary>
public sealed class NpsCollectionStatusTests
{
    [Fact]
    public void Derive_should_return_answered_when_the_project_has_a_response()
    {
        Assert.Equal(NpsCollectionStatus.Answered, NpsCollectionStatusCalculator.Derive(hasResponse: true, activeDispatches: 0));
    }

    [Fact]
    public void Derive_should_prefer_answered_over_an_open_link()
    {
        Assert.Equal(NpsCollectionStatus.Answered, NpsCollectionStatusCalculator.Derive(hasResponse: true, activeDispatches: 2));
    }

    [Fact]
    public void Derive_should_return_link_sent_when_there_is_an_open_dispatch_and_no_response()
    {
        Assert.Equal(NpsCollectionStatus.LinkSent, NpsCollectionStatusCalculator.Derive(hasResponse: false, activeDispatches: 1));
    }

    [Fact]
    public void Derive_should_return_pending_when_there_is_neither_link_nor_response()
    {
        Assert.Equal(NpsCollectionStatus.Pending, NpsCollectionStatusCalculator.Derive(hasResponse: false, activeDispatches: 0));
    }
}
