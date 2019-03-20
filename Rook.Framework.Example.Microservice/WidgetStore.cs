using System.Collections.Generic;
using Rook.Framework.Example.Microservice.Objects;

namespace Rook.Framework.Example.Microservice
{
    public static class WidgetStore
    {
        private static readonly List<Widget> Widgets = new List<Widget>();
        
        public static void Put(Widget w)
        {
            Widgets.Add(w);
        }

        public static IEnumerable<Widget> Get()
        {
            return Widgets;
        }
    }
}