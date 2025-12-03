using Microsoft.Maui.Controls;

namespace NoveoNative
{
    public class MessageDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? IncomingTemplate { get; set; }
        public DataTemplate? OutgoingTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is MessageViewModel message)
            {
                return message.IsMine
                    ? (OutgoingTemplate ?? new DataTemplate())
                    : (IncomingTemplate ?? new DataTemplate());
            }
            return new DataTemplate();
        }
    }
}