using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;

using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using System.IO;

namespace TurbinaAlpha.Data
{
    public class DAO
    {
        public static Turbina obtenerInfo(ConversationData.Turbina turbina)
        {
            Random random = new Random();
            Turbina tur = new Turbina { amperaje = random.Next(0, 5), voltaje = random.Next(0, 13), carga = random.Next(0, 100), rpm = random.Next(3, 210) };
            switch (turbina)
            {
                case ConversationData.Turbina.TurA:
                    tur.nombre = "Arthas";
                    break;
                case ConversationData.Turbina.TurB:
                    tur.nombre = "Berta";
                    break;
                case ConversationData.Turbina.TurC:
                    tur.nombre = "CarlaMagna";
                    break;
                    //No debieran ocurrir
                case ConversationData.Turbina.todas:
                    tur.nombre = "Todas";
                    break;
                case ConversationData.Turbina.ninguna:
                    tur.nombre = "Ninguna";
                    break;
            }
            return tur;
        }

        public static async Task<Anemometro> obtenerVientoAsync()
        {
            //Obtener el blob
            CloudStorageAccount storageAccount = new CloudStorageAccount(
                   new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
                   "blobunlam", "GrFvbB1NlpHgdGq1IK/PkMRug85YxDT9qtjGFBNTq2bwI/QiQE/ufZs538dTt2txhSLCoE+tauW3Y+7grf8f2A=="), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("anemometro");

            Microsoft.WindowsAzure.Storage.Auth.StorageCredentials storageCredentials = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials("blobunlam", "GrFvbB1NlpHgdGq1IK/PkMRug85YxDT9qtjGFBNTq2bwI/QiQE/ufZs538dTt2txhSLCoE+tauW3Y+7grf8f2A==");
            DateTime date = DateTime.Now.Subtract(new TimeSpan(0,1,0));

            CloudBlockBlob miBloque = new CloudBlockBlob(new Uri(container.Uri.ToString() + $"/Anemometro/01/2019/02/13/18/43"), storageCredentials);
           // CloudBlockBlob miBloque = new CloudBlockBlob(new Uri(container.Uri,$"0/{date.Year}/{date.Month}/{date.Day}/{date.Hour}/{date.Minute}"), storageCredentials);
            //Tomo el texto del bloque
            string miTexto = null;
            bool existe = false;

            try
            {
                existe = await miBloque.ExistsAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (existe)
            {
                miTexto = await miBloque.DownloadTextAsync();
                var serializer = AvroSerializer.Create<Object>();
                System.IO.Stream stream = new MemoryStream();
                //serializer.Deserialize(await miBloque.DownloadToStreamAsync(new MemoryStream(50)));
                
            }
            else
            {
                return null;
            }
            //Parsearlo
            int indice = miTexto.LastIndexOf("\"velocidad\":");
            float velocidad = float.Parse(miTexto.Substring(indice, 8));
            return new Anemometro { nombre = "nimometro", velocidad = velocidad };
            

        }
        public static bool obtenerEstado(ConversationData.Turbina turbina)
        {
            return true;
        }

        public static List<Turbina> obtenerInfoTodas()
        {
            Random random = new Random();
            List<Turbina> turbinas = new List<Turbina>();
            Turbina A, B, C;
            A = new Turbina { amperaje = random.Next(0, 5), voltaje = random.Next(0, 13), carga = random.Next(0, 100), rpm = random.Next(3, 210) };
            B = (Turbina)A.Clone();
            C = (Turbina)A.Clone();
            A.nombre = "Arthas";
            B.nombre = "Berta";
            C.nombre = "Carla(Magna)";
            
            turbinas.Add(A);
            turbinas.Add(B);
            turbinas.Add(C);
            return turbinas;
        }

        public static Dictionary<string,bool> obtenerEstados() //Esto es básicamente un HashMap, cuenta con Clave y valor.
        {
            Dictionary<string,bool> estados = new Dictionary<string, bool>();
            estados.Add("Arthas", true);
            estados.Add("Berta", true);
            estados.Add("Carlamagna", true);
            return estados;
        }
        public static bool obtenerEstadoTurbina(ConversationData.Turbina turbina)
        {
            switch (turbina)
            {
                case ConversationData.Turbina.TurA:
                    return true;
                case ConversationData.Turbina.TurB:
                    return true;
                case ConversationData.Turbina.TurC:
                    return true;
                case ConversationData.Turbina.todas:
                    var estados = obtenerEstados();
                    foreach(var estado in estados)
                    {
                        if (!estado.Value)
                            return false;
                    }
                    return true;
                //No debería recibir la otras opciones
                default:
                    throw new Exception("Uso equivocado");
            }
        }
    }
}
