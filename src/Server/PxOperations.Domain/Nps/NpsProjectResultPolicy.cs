namespace PxOperations.Domain.Nps;

public static class NpsProjectResultPolicy
{
    public static NpsProjectResultStatus DetermineStatus(bool hasResponses, bool hasOpenDispatch)
    {
        if (hasResponses)
        {
            return NpsProjectResultStatus.Responded;
        }

        return hasOpenDispatch
            ? NpsProjectResultStatus.LinkGenerated
            : NpsProjectResultStatus.Pending;
    }
}
