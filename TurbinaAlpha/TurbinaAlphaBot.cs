// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using TurbinaAlpha.Accessors;
using TurbinaAlpha.Data;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TurbinaAlpha
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// Por cada interacción del usuario, una instancia de esta clase es creada cuando el método OnTurnAsync es llamado.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class TurbinaAlphaBot : IBot //Hereda de la clase IBot
    {
        private readonly StateBotAccessors _accessors;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>            
        
        public TurbinaAlphaBot(StateBotAccessors accessors, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<StateBotAccessors>();
            _logger.LogTrace("TurbinaAlpha turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
        }

        /// <summary>
        /// Every conversation turn calls this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// 
        
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            //obtengo la informacion de la conversación
            var conversationData = await _accessors.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData());

            //Le doy un mensaje de bienvenida si no fue bienvenido
            if (conversationData.saludado == false && turnContext.Activity.Type == ActivityTypes.ConversationUpdate && turnContext.Activity.MembersAdded.Count > 0)
            {
                foreach (var member in turnContext.Activity.MembersAdded)
                {
                    if (member.Id != turnContext.Activity.Recipient.Id)
                    {
                        conversationData.saludado = true;
                        string turbinasFuncionando = generarMensajeEstadoTurbinas(intro: true, outro: true);
                        if (string.IsNullOrEmpty(turbinasFuncionando))
                            await turnContext.SendActivityAsync($"Hola {member.Name}, no hay ninguna turbina funcionando. Eventualmente te podré decir cuanto generan, giran, su carga y la velocidad del viento");
                        else
                            await turnContext.SendActivityAsync($"Hola {member.Name}, {turbinasFuncionando}     Te puedo decir:" +
                                                                                                                $"\n● Cuanto generan (V/A)" +
                                                                                                                $"\n● Cuanto giran (RPM)" +
                                                                                                                $"\n● Velocidad del viento (Km/h)" +
                                                                                                                $"\n● Porcentaje de carga de batería (%)");
                        await _accessors.ConversationDataAccessor.SetAsync(turnContext, conversationData);
                        await _accessors.ConversationState.SaveChangesAsync(turnContext);
                    }
                }
            }
            //Manejo el mensaje
            if (turnContext.Activity.Type == ActivityTypes.Message /*&& conversationData.saludado == true*/)
            {
                
                if (IsMostlyUpper(turnContext.Activity.Text))
                    await turnContext.SendActivityAsync("No me grite");
                    
                var mensaje = turnContext.Activity.Text.Trim().ToLower();

                if (Regex.IsMatch(mensaje, "ayuda|help"))
                {
                    string turbinasFuncionando = generarMensajeEstadoTurbinas(true, false);
                    if (string.IsNullOrEmpty(turbinasFuncionando))
                        await turnContext.SendActivityAsync("Ahora mismo no hay ninguna turbina funcionando");
                    else
                        await turnContext.SendActivityAsync("Me podés preguntar por " + turbinasFuncionando
                            + " acerca de cuanto giran, generan o su nivel de batería además de la velocidad del viento");
                    return;
                }
                //Determino de que turbina me habla (solo considero una o todas)
                ConversationData.Turbina turbina = conversationData.turbina; //Asumo que es la misma que la vez anterior
                bool turA, turB, turC, todas = turC = turB = turA = false;

                //Busco si menciona alguna
                if (Regex.IsMatch(mensaje, "arthas"))
                {
                    turA = true;
                    turbina = ConversationData.Turbina.TurA;
                }
                if (Regex.IsMatch(mensaje, "(turbina )?berta"))
                {
                    turB = true;
                    turbina = ConversationData.Turbina.TurB;
                }
                if (Regex.IsMatch(mensaje, "(turbina )?carla(magna)?"))
                {
                    turC = true;
                    turbina = ConversationData.Turbina.TurC;
                }
                //Si no menciona alguna veo si se refiere a todas
                if ((!turA && !turB && !turC && Regex.IsMatch(mensaje, "turbinas|tod(e|a)s")) || (turA && turB && turC))
                {
                    turbina = ConversationData.Turbina.todas;
                    todas = true;
                }
                //Reviso que haya solo una mencion y si la hubo
                if (!(turA ^ turB ^ turC) && !todas && (turA || turB || turC || todas))
                {
                    await turnContext.SendActivityAsync("Solo puedo decir de una turbina o todas, no de a pares :)");
                    return;
                }
                //Determino si tengo que contestar por algun par.
                /* Verificar cuales fueron mencionadas y guardarlas en una variable para procesar la respuesta
                 * Al momento el enum de turbina no cuenta con las turbinas en pares
                 * Habría que evitar usar enum y quizá usar una lista
                 * */
                //Determino que quiere
                List<ConversationData.Opcion> opciones = new List<ConversationData.Opcion>();

                if (Regex.IsMatch(mensaje, "estado(s)?|funcion(a|ando)?|and(a|ando|an)?"))
                    opciones.Add(ConversationData.Opcion.estado);
                if (Regex.IsMatch(mensaje, "gener(ando|a|an)?|produc(e|en|iendo)"))
                    opciones.Add(ConversationData.Opcion.genera);
                if (Regex.IsMatch(mensaje, "carga|nivel|bater(i|í)a|pila|energ(i|í)a"))
                    opciones.Add(ConversationData.Opcion.carga);
                if (Regex.IsMatch(mensaje, "gira(ndo|n)?|re(v|b)oluci(o|ó)n(es)?|vuelta|rpm"))
                    opciones.Add(ConversationData.Opcion.revolución);
                if (Regex.IsMatch(mensaje, "viento|brisa|aire|sopla(ndo)?|ventisca"))
                    opciones.Add(ConversationData.Opcion.viento);

                //Si no eligió opción
                if (opciones.Count == 0)
                {
                    var opcionAnterior = conversationData.opciones ?? new List<ConversationData.Opcion>(); //recupero la elección anterior
                    if (opcionAnterior.Count == 0) //si no hay elección anterior
                    {
                        await turnContext.SendActivityAsync("¿Qué desea saber? ¿Viento, generacion, nivel de batería o revoluciones?"); //pregunto por la opción
                        if (turbina == ConversationData.Turbina.ninguna)//Si no eligió turbina
                            await turnContext.SendActivityAsync("¿Y de qué turbina?¿Arthas, Berta, Carla o todas?"); //se lo recuerdo
                        return;
                    }
                    opciones = opcionAnterior;
                }
                else
                {
                    conversationData.opciones = opciones; //guardo la opcion elegida
                }
                //Obtener la informacion solicitada y responder
                var respuesta = await generarRespuestaAsync(turbina, opciones);
                //Testing
                /*
                respuesta = "Seleccionaste: " + turbina + "\n Opciones:";
                foreach(var opcion in opciones)
                {
                    respuesta += (opcion + " ");
                }*/

                await turnContext.SendActivityAsync(respuesta);
                //Guardo los datos de la conversacion
                conversationData.turbina = turbina;
                await _accessors.ConversationDataAccessor.SetAsync(turnContext, conversationData);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
            }
        }

        private string generarMensajeEstadoTurbinas(bool intro, bool outro)
        {
            var estados = DAO.obtenerEstados();
            foreach (var item in estados.Where(kvp => kvp.Value == false).ToList())
            {
                estados.Remove(item.Key); //Elimino las turbinas que no están andando
            }
            string mensaje = "";
            var turbinasFuncionando = estados.Keys;
            if (turbinasFuncionando.Count >= 2)
            { //Plural
                mensaje = (intro ? "las turbinas " : "") + string.Join(", ", turbinasFuncionando) + (outro ? " están generando." : "");
                //Reemplazo la ultima coma por "y"
                int ultimaComa = mensaje.LastIndexOf(',');
                mensaje = mensaje.Remove(ultimaComa, 1).Insert(ultimaComa, " y");
                return mensaje;
            }
            if (turbinasFuncionando.Count == 1)
            { //Singular
                mensaje = (intro ? "la turbina " : "") + turbinasFuncionando.ElementAt(0) + (outro ? " está generando." : "");
                return mensaje;
            }
            //if (turbinasFuncionando.Count < 1) //No hay turbinas
            return null;

        }

        private async Task<string> generarRespuestaAsync(ConversationData.Turbina turbina, List<ConversationData.Opcion> opciones)
        {
            string response = "";
            bool viento = opciones.Contains(ConversationData.Opcion.viento); //si preguntó por el viento lo guardo
            if (viento)
                opciones.Remove(ConversationData.Opcion.viento); //y lo saque de la lista para atender
            //Si lo que preguntó requiere alguna turbina y no mencionó alguna
            if (turbina == ConversationData.Turbina.ninguna && !viento && opciones.Count > 0)
                return "No indicó la turbina, están Arthas, Berta y Carla(Magna)";

            //Si pregunta por el estado y alguna información inherente a la turbina que implica un estado correcto o si pregunta por información de la turbina
            if ((opciones.Contains(ConversationData.Opcion.estado) && opciones.Count > 1) || (!opciones.Contains(ConversationData.Opcion.estado) && opciones.Count > 0))
            {
                string textoTurbina = "";
                List<Turbina> info = new List<Turbina>();
                if (turbina == ConversationData.Turbina.todas)
                    info = DAO.obtenerInfoTodas();
                else
                    info.Add(DAO.obtenerInfo(turbina));
                opciones.Remove(ConversationData.Opcion.estado);
                foreach (var item in info) //para cada turbina en mi lista
                {
                    if (opciones.Count >= 3)
                    {
                        textoTurbina = $"La turbina {item.nombre} está: \n";
                        foreach (var opcion in opciones)
                        {
                            switch (opcion)
                            {
                                case ConversationData.Opcion.carga:
                                    textoTurbina += $"Cargada al %{item.carga}. \n";
                                    break;
                                case ConversationData.Opcion.genera:
                                    textoTurbina += $"Generando {item.amperaje} A a {item.voltaje} V. \n";
                                    break;
                                case ConversationData.Opcion.revolución:
                                    textoTurbina += $"Girando a {item.rpm} rpm. \n";
                                    break;
                            }
                        }
                    }
                    else
                    {
                        textoTurbina = $"La turbina {item.nombre} está ";
                        List<string> textoOpciones = new List<string>();
                        foreach (var opcion in opciones)
                        {
                            switch (opcion)
                            {
                                case ConversationData.Opcion.carga:
                                    textoOpciones.Add($"cargada al %{item.carga}");
                                    break;
                                case ConversationData.Opcion.genera:
                                    textoOpciones.Add($"generando {item.amperaje} A a {item.voltaje} V");
                                    break;
                                case ConversationData.Opcion.revolución:
                                    textoOpciones.Add($"girando a {item.rpm} rpm");
                                    break;
                            }
                        }
                        textoTurbina += string.Join(" y ", textoOpciones) + ".\n";
                    }
                    response += textoTurbina;
                }
            }
            else if (opciones.Contains(ConversationData.Opcion.estado))//Si pregunta solo por el estado
            {
                if (turbina == ConversationData.Turbina.todas)
                {
                    var estados = generarMensajeEstadoTurbinas(true, true);
                    if (string.IsNullOrEmpty(estados))
                        response = "No hay turbina disponible. \n";
                    else
                        response = estados + "\n";
                }
                else
                {
                    var estado = DAO.obtenerEstadoTurbina(turbina);
                    switch (turbina)
                    {
                        case ConversationData.Turbina.TurA:
                            response += "La turbina Arthas " + (estado ? "" : "no ") + "está generando. \n";
                            break;
                        case ConversationData.Turbina.TurB:
                            response += "La turbina Berta " + (estado ? "" : "no ") + "está generando. \n";
                            break;
                        case ConversationData.Turbina.TurC:
                            response += "La turbina Carlamagna " + (estado ? "" : "no ") + "está generando. \n";
                            break;
                    }
                }

            }
            if (viento) //El viento es común a las turbinas
            {
                Anemometro infoViento = await DAO.obtenerVientoAsync();
                if (null != infoViento)
                    response += $"Hay {infoViento.velocidad} m/s de viento";
                else
                    response += "Amnemómetro no disponible";
            }
            return response.Trim();
        }
        bool IsMostlyUpper(string input)
        {
            int upper = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (Char.IsUpper(input[i]))
                    upper++;
            }
            return (upper > (input.Length / 2));
        }
    }
}
