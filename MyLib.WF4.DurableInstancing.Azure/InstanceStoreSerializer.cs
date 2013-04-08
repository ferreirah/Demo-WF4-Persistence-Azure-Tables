using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace MyLib.WF4.DurableInstancing
{
    internal static class InstanceStoreSerializer
    {
        #region "Constantes"
        
        private const String c_XmlRoot = "Instance";
        private const String c_XmlNode = "Node";
        private const String c_XmlKey = "Key";
        private const String c_XmlValue = "Value";
        private const String c_XmlOptions = "Options";

        #endregion
        
        #region "Public Methods"

        /// <summary>
        /// Retourner les data pour les persister
        /// </summary>
        /// <param name="instanceData"></param>
        /// <param name="instanceMetaData"></param>
        /// <param name="docData"></param>
        /// <param name="docMetaData"></param>
        public static void GetDataToSave( IDictionary<XName, InstanceValue> instanceData, IDictionary<XName, InstanceValue> instanceMetaData, out XDocument docData, out XDocument docMetaData)
        {
            NetDataContractSerializer serializer = new NetDataContractSerializer();

            docData = new XDocument(
                new XElement(c_XmlRoot,
                        GetNodesToSave(serializer, instanceData)
                        ));

            docMetaData = new XDocument(
                new XElement(c_XmlRoot,
                        GetNodesToSave(serializer, instanceMetaData)
                        ));

            serializer = null;
        }

        /// <summary>
        /// Retourner les data pour restaurer une instance de workflows
        /// </summary>
        /// <param name="docData"></param>
        /// <param name="docMetaData"></param>
        /// <param name="instanceData"></param>
        /// <param name="instanceMetaData"></param>
        public static void GetDataToLoad(XDocument docData, XDocument docMetaData, out IDictionary<XName, InstanceValue> instanceData, out IDictionary<XName, InstanceValue> instanceMetaData)
        {
            NetDataContractSerializer serializer = new NetDataContractSerializer();

            // Recherche des Data de l'instance
            instanceData = GetNodesToLoad(
                serializer,
                docData.Element(c_XmlRoot)
                    .Elements(c_XmlNode)
                    .ToArray());

            // Recherche des MetaData de l'instance
            instanceMetaData = GetNodesToLoad(
                serializer,
                docMetaData.Element(c_XmlRoot)
                    .Elements(c_XmlNode)
                    .ToArray());

            serializer = null;
        }

        #endregion

        #region "Serialization"

        /// <summary>
        /// Chargement des nodes de données d'une instance
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static IDictionary<XName, InstanceValue> GetNodesToLoad(
            NetDataContractSerializer serializer,
            XElement[] nodes)
        {
            IDictionary<XName, InstanceValue> result = new Dictionary<XName, InstanceValue>();
            foreach (XElement node in nodes)
            {
                Object key = Deserialize(serializer, node.Element(c_XmlKey));
                Object value = Deserialize(serializer, node.Element(c_XmlValue));
                Object options = Deserialize(serializer, node.Element(c_XmlOptions));

                result.Add(
                    key as XName,
                    new InstanceValue(value, (InstanceValueOptions)options));
            }
            return result;
        }

        /// <summary>
        /// Retourner les nodes à pesister
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static XElement[] GetNodesToSave(NetDataContractSerializer serializer, IDictionary<XName, InstanceValue> data)
        {
            return data.Where(node =>
                    !node.Value.IsDeletedValue
                    && !node.Value.Options.HasFlag(InstanceValueOptions.WriteOnly))
                .Select(node => new XElement(c_XmlNode,
                    Serialize(serializer, c_XmlKey, node.Key),
                    Serialize(serializer, c_XmlValue, node.Value.Value),
                    Serialize(serializer, c_XmlOptions, node.Value.Options)))
                .ToArray();
        }

        /// <summary>
        /// Sérilizer un element pour le persistance
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static XElement Serialize(NetDataContractSerializer serializer, String name, Object value)
        {
            XElement element = new XElement(name);

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(stream, value);
            stream.Position = 0;

            StreamReader reader = new StreamReader(stream);
            element.Add(XElement.Load(stream));

            // Libération des ressources
            reader.Close();
            reader.Dispose();
            stream.Dispose();

            return element;
        }

        /// <summary>
        /// Desserilizer un element pour le persistance
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private static Object Deserialize(NetDataContractSerializer serializer, XElement element)
        {
            Object result = null;
            MemoryStream stream = new MemoryStream();

            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(stream);

            foreach (XNode node in element.Nodes())
            {
                node.WriteTo(writer);
            }
            writer.Flush();
            stream.Position = 0;
            result = serializer.Deserialize(stream);

            // Libération des ressources
            writer.Close();
            stream.Dispose();

            return result;
        }

        #endregion
    }
}
