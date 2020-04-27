using System;
using Microsoft.Azure.EventGrid.Models;

namespace NosAyudamos
{
    static class EventGridExtensions
    {
        public static T GetData<T>(this EventGridEvent e, ISerializer serializer)
        {
            if (e.EventType != typeof(T).FullName)
                throw new NotSupportedException($"Expected {typeof(T).FullName} as event type but got {e.EventType}");

            return serializer.Deserialize<T>(e.Data);
        }
    }
}
