using System;

namespace TurbinaAlpha.Data
{
    /// <summary>
    ///  Clase Turbina
    ///  
    /// Nombre: Nombre de la turbina
    /// RPM: Cantidad de vueltas que dan por minuto. -> Tampoco está el cálculo hecho, ni siquiera envía datos la turbina, todavía.
    /// Carga: Carga de la batería -> No se como quieren calcular el porcentaje de la batería.
    /// Voltaje: El voltaje que la turbina está generado, o por lo menos el último valor que se tiene registrado.
    /// Amperaje: El amperaje que la turbina está generando, o por lo menos el último valor que se tiene registrado.
    /// </summary>
    public class Turbina : ICloneable
    {
        public string nombre { get; set; } 
        public float rpm { get; set; }
        public float carga { get; set; }
        public float voltaje { get; set; }
        public float amperaje { get; set; }
        

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

}
