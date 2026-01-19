using UnityEngine;

namespace N2K
{
    public class InfoBoxAttribute : PropertyAttribute
    {
        public readonly string message;
        public readonly MessageType type;

        public InfoBoxAttribute(string message, MessageType type = MessageType.Warning)
        {
            this.message = message;
            this.type = type;
        }
    }

    public enum MessageType
    {
        None,
        Info,
        Warning,
        Error
    }
}