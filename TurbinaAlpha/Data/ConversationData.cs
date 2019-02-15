using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TurbinaAlpha.Data
{
    public class ConversationData
    {
        public enum Turbina
        {
            TurA,
            TurB,
            TurC,
            todas,
            ninguna,
        }

        public enum Opcion
        {
            genera,
            revolución,
            viento,
            carga,
            estado
        }

        public Turbina turbina { get; set; } = Turbina.ninguna;

        public List<Opcion> opciones {get;set;}

        public bool saludado { get; set; } = false;
    }
}
