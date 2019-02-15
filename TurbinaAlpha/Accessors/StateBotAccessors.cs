using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurbinaAlpha.Data;

namespace TurbinaAlpha.Accessors
{
    public class StateBotAccessors
    {
        public StateBotAccessors(ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public static string ConversationDataName { get; } = "ConversationData";

        public IStatePropertyAccessor<ConversationData> ConversationDataAccessor { get; set; }

        public ConversationState ConversationState { get; }

    }
}
