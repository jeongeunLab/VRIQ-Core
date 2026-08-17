using System;

namespace VRIQ.Data
{
    [Serializable]
    public sealed class InteractionEventRecord
    {
        public string schemaVersion = "0.1.0";

        public long sessionTimeNs;
        public int frameIndex;

        public string eventType;
        public string interactionId;
        public string phase;

        public string targetId;
        public string actionId;
        public string outcomeId;
    }
}