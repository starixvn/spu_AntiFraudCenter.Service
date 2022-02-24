namespace Fanex.NAP.SPU.AFC.CC.Features.SyncAFCData
{
    using Fanex.Data.Repository;

    public class ParticipantGetCriteria : CriteriaBase
    {
        public override string GetSettingKey()
        {
            return "AFC_DC_Participant_Get";
        }

        public override bool IsValid()
        {
            return true;
        }
    }
}