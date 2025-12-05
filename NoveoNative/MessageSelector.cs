using Microsoft.Maui.Controls;

namespace NoveoNative
{
    public class MessageDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? IncomingMessageTemplate { get; set; }
        public DataTemplate? OutgoingMessageTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is MessageViewModel message)
            {
                return message.IsOutgoing
                    ? (OutgoingMessageTemplate ?? new DataTemplate())
                    : (IncomingMessageTemplate ?? new DataTemplate());
            }
            return new DataTemplate();
        }
    }
}
